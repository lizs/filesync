#pragma once

#include <functional>
class WebClient
{
public:
	void DownloadStringCompleted(std::function<void(bool, const std::string &)> cb){}
	void download_string_async(const std::string& cs){}
};
