#include "FileSynchronizer.h"

#include <boost/filesystem.hpp>
#include <boost/property_tree/ptree.hpp>
#include <boost/property_tree/json_parser.hpp>

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
			auto webClient = new WebClient();
			webClient->DownloadStringCompleted([&cb, this](bool success, const std::string& msg)
				{
					if (!success)
					{
						cb(success, msg);
						return;
					}

					parse_remote_md5(msg);

					auto diff = make_diff();
					if (m_differencesGot != nullptr)
						m_differencesGot(diff);

					download(diff);
					clear();
					cb(true, "ok");
				});

			webClient->download_string_async(m_url + m_remoteMd5Path);
		}
		catch (...)
		{
			cb(false, "Exception catched!");
		}
	}

	void FileClient::download(const std::vector<std::pair<std::string, int>>& diff)
	{
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
			//remove_empty_directories(dir.path);

			if (!filesystem::is_empty(dir))
				continue;

			filesystem::remove_all(root);
		}
	}

	void FileClient::download(std::vector<std::string>& files)
	{
		WebClient downloader;
		for (auto path : files)
		{
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

		//property_tree::ptree tree;		
		//property_tree::json_parser::read_json(text, tree);

		//auto root = tree.get_child("root");
		//for (auto kv : root)
		//{
		//	//m_remoteMd5Map[kv.first] = kv.second;
		//}

		//var data = JsonMapper.ToObject(jsonText);
		//foreach(var key in((IDictionary)data).Keys)
		//{
		//	var k = (string)key;
		//	var dic = (IDictionary)data[k];
		//	_remoteMd5Map[k] = new Dictionary<string, string>
		//	{
		//		{ "md5", dic["md5"].ToString() },
		//		{ "size", dic["size"].ToString() }
		//	};
		//}

		//if (OnRemoteMd5 != null)
		//	OnRemoteMd5(_remoteMd5Map);



		//// Use the throwing version of get to find the debug filename.
		//// If the path cannot be resolved, an exception is thrown.
		//m_file = tree.get<std::string>("debug.filename");

		//// Use the default-value version of get to find the debug level.
		//// Note that the default value is used to deduce the target type.
		//m_level = tree.get("debug.level", 0);

		//// Use get_child to find the node containing the modules, and iterate over
		//// its children. If the path cannot be resolved, get_child throws.
		//// A C++11 for-range loop would also work.
		//BOOST_FOREACH(pt::ptree::value_type &v, tree.get_child("debug.modules")) {
		//	// The data function is used to access the data stored in a node.
		//	m_modules.insert(v.second.data());
		//}
	}

	void FileClient::scan_local_md5()
	{
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
