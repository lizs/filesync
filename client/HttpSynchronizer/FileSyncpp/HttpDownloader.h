#pragma once

#include <functional>
#include "cocos/cocos2d.h"
#include "cocos/network/CCDownloader.h"

namespace network
{
	class HttpDownloader
	{
		std::unique_ptr<cocos2d::network::Downloader> m_downloader;
		std::string m_localPath;
		std::string m_url;
		std::atomic<size_t> m_counter;
	public:
		HttpDownloader(const std::string& localpath, const std::string& url);
		void download_async(const std::vector<std::string> & files, std::function<void(bool, const std::string&)> cb);
		void download_string_async(const std::string & md5_url, std::function<void(bool, const std::string&)> cb);

	private:
		void download_async(const std::string& relativePath, std::function<void(bool, const std::string&)> cb);
	};
}
