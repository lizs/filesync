#include "FileSynchronizer.h"

#include <string>
#include <boost/filesystem.hpp>
#include <boost/property_tree/ptree.hpp>
#include <boost/property_tree/json_parser.hpp>
#include "md5.h"
#include "HttpDownloader.h"

namespace network
{
	FileClient::FileClient(const char* url, const char* remoteMd5Path, const char* localPath)
	{
		m_url = url;
		m_remoteMd5Path = remoteMd5Path;
		m_localPath = localPath;

		if (!filesystem::is_directory(m_localPath))
			throw std::runtime_error("invalid local path specified!");

		if (!filesystem::exists(m_localPath))
			filesystem::create_directory(m_localPath);
	}

	void FileClient::sync(std::function<void(bool, const std::string&)> cb)
	{
		try
		{
			scan_local_md5();

			HttpDownloader downloader(m_localPath, m_url + m_remoteMd5Path);
			// get remote md5 map
			downloader.download_string_async(m_url + m_remoteMd5Path, [&cb, this](bool success, const std::string& msg)
				{
					if (!success && cb != nullptr)
					{
						cb(false, msg);
						return;
					}

					parse_remote_md5(msg);

					auto diff = make_diff();
					if (m_differencesGot != nullptr)
						m_differencesGot(diff);

					// update
					download(diff, [this, &cb](bool success, const std::string& message) {
						if(cb != nullptr)
						{
							if (success)
							{
								clear();
								cb(true, "ok");
							}
							else
							{
								cb(false, message);
							}
						}
					});
				});
		}
		catch (...)
		{
			cb(false, "Exception catched!");
		}
	}

	void FileClient::download(const std::vector<std::pair<std::string, int>>& diff, std::function<void(bool, const std::string&)> cb)
	{
		if(diff.empty())
			return;

		auto expiredFiles = std::vector<std::string>();
		for(auto & kv : diff)
		{
			expiredFiles.push_back(kv.first);
		}

		HttpDownloader downloader(m_localPath, m_url);
		downloader.download_async(expiredFiles,  [this, &cb](bool success, const std::string& message){
			if(cb != nullptr)
			{
				cb(success, message);
			}
		});
	}

	void FileClient::remove_expired_files()
	{
		std::vector<std::string> unused;
		for (auto kv : m_localMd5Map)
		{
			if (m_remoteMd5Map.find(kv.first) == m_remoteMd5Map.end())
			{
				unused.push_back(m_localPath + kv.first);
			}
		}

		for (auto file : unused)
		{
			if (filesystem::exists(file))
			{
				filesystem::remove(file);

				if (m_fileDelete != nullptr)
					m_fileDelete(file);
			}
		}
	}

	void FileClient::remove_empty_directories(const std::string& root)
	{
		for (auto& dir : filesystem::directory_iterator(root))
		{
			remove_empty_directories(dir.path().string());

			if (!filesystem::is_empty(dir))
				continue;

			filesystem::remove_all(root);
		}
	}
	
	void FileClient::clear()
	{
		scan_local_md5();
		remove_expired_files();
		remove_empty_directories(m_localPath);
	}

	void FileClient::parse_remote_md5(const std::string& text)
	{
		m_localMd5Map.clear();

		property_tree::ptree tree;
		std::stringstream oss;
		oss << text;
		property_tree::read_json(oss, tree);

		for (auto & kv : tree.get_child("root"))
		{
			auto node = kv.second;
			auto path = node.get<std::string>("path");
			auto size = node.get<int>("size");
			auto md5 = node.get<std::string>("md5");

			auto innerMap = std::map<std::string, std::string>();
			innerMap["md5"] = md5;

			std::stringstream ss;
			ss << size;

			innerMap["size"] = ss.str();

			m_remoteMd5Map[path] = innerMap;
		}

		if (m_remoteMd5Got != nullptr)
			m_remoteMd5Got(m_remoteMd5Map);
	}

	void FileClient::scan_local_md5()
	{
		m_localMd5Map.clear();
		
		scan_local_md5(m_localPath);

		if (m_localMd5Got != nullptr)
			m_localMd5Got(m_localMd5Map);
	}

	void FileClient::scan_local_md5(const std::string & path)
	{
		for (auto& entry : filesystem::directory_iterator(path))
		{
			if (filesystem::is_regular_file(entry))
			{
				auto refPath = filesystem::relative(entry, m_localPath);

				std::fstream file(filesystem::unique_path(entry).string(), std::ios::in | std::ios::binary | std::ios::ate);
				if(!file.is_open())
				{
					throw std::runtime_error(entry.path().string() + "couldn't open!");
				}

				size_t size = file.tellg();
				file.seekg(0, std::ios::beg);
				std::auto_ptr<char> bytes(new char[size]);

				m_localMd5Map[refPath.string()] = std::map<std::string, std::string>
				{
					{ "md5", MD5(bytes.get()).toStr() },
					{ "size", to_string(size) }
				};

			}
			else if(filesystem::is_directory(entry))
			{
				scan_local_md5(entry.path().string());
			}
		}
	}

	std::vector<std::pair<std::string, int>> FileClient::make_diff()
	{
		std::vector<std::pair<std::string, int>> ret;
		for (auto kv : m_remoteMd5Map)
		{
			if (m_localMd5Map.find(kv.first) != m_localMd5Map.end() || m_remoteMd5Map[kv.first]["md5"] != m_localMd5Map[kv.first]["md5"])
			{
				ret.push_back(std::make_pair(kv.first, atoi(m_remoteMd5Map[kv.first]["size"].c_str())));
			}
		}

		return ret;
	}
}
