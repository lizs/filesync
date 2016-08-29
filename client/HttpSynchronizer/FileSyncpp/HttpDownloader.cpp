#include "HttpDownloader.h"

namespace network
{
	HttpDownloader::HttpDownloader(const std::string& localpath, const std::string& url)
	{
		m_downloader.reset(new cocos2d::network::Downloader());
		m_localPath = localpath;
		m_url = url;

		m_counter = 0;
	}

	void HttpDownloader::download_async(const std::vector<std::string> & paths, std::function<void(bool, const std::string&)> cb)
	{
		if(paths.empty())
		{
			cb(true, "ok");
			return;
		}

		m_counter = paths.size();
		for (auto & path : paths)
		{
			download_async(path, [this, &cb](bool success, const std::string& msg)
			{
				if(success)
				{
					--m_counter;
					if(0 <= m_counter)
					{
						cb(true, "ok");
					}
				}
				else
				{
					cb(false, msg);
				}
			});
		}
	}

	void HttpDownloader::download_string_async(const std::string& md5_url, std::function<void(bool, const std::string&)> cb)
	{
		m_downloader->onDataTaskSuccess = [this, &cb](const cocos2d::network::DownloadTask& task,
			std::vector<unsigned char>& data)
		{
			if (cb != nullptr)
			{
				auto md5 = std::string("");
				const char *p = reinterpret_cast<char *>(data.data());
				md5.insert(md5.end(), p, p + data.size());

				cb(true, md5);
			}
		};

		m_downloader->onTaskError = [this, &cb](const cocos2d::network::DownloadTask& task,
			int errorCode,
			int errorCodeInternal,
			const std::string& errorStr)
		{
			if (cb != nullptr)
			{
				cb(false, errorStr);
			}
		};

		m_downloader->createDownloadDataTask(md5_url, "md5");
	}

	void HttpDownloader::download_async(const std::string& relativePath, std::function<void(bool, const std::string&)> cb)
	{
		auto path = m_localPath + relativePath;
		auto url = m_url + relativePath;

		m_downloader->onFileTaskSuccess = [this, &cb](const cocos2d::network::DownloadTask& task)
		{
			if (cb != nullptr)
			{
				cb(true, "ok");
			}
		};

		m_downloader->onTaskError = [this, &cb](const cocos2d::network::DownloadTask& task,
			int errorCode,
			int errorCodeInternal,
			const std::string& errorStr)
		{
			if (cb != nullptr)
			{
				cb(false, errorStr);
			}
		};

		m_downloader->createDownloadFileTask(url, path, relativePath);
	}
}
