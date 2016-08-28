#ifndef  _FILE_SYNCHRONIZER_H
#define _FILE_SYNCHRONIZER_H
#include <string>
#include <map>
#include <functional>

#include "WebClient.h"
#include <vector>
#include <boost/system/error_code.hpp>

using namespace boost;

namespace network
{
	class FileClient
	{
	private:
		typedef std::map<std::string, std::map<std::string, std::string>> Md5Map;
		system::error_code m_lastError;

#pragma region json structure
		struct FileDetails {
			std::string RelativePath;
			int Size;
			std::string Md5;
		};
#pragma regionend

#pragma region callbacks
		std::function<void(Md5Map)> m_remoteMd5Got;
		std::function<void(Md5Map)> m_localMd5Got;
		std::function<void(std::string)> m_fileCreate;
		std::function<void(std::string)> m_fileDelete;
		std::function<void(std::string)> m_folderDelete;
		std::function<void(std::vector<std::pair<std::string, int>>)> m_differencesGot;
#pragma endregion 

	public:
		const system::error_code & getLastError() const
		{
			return m_lastError;
		}

#pragma region callbacks register
		void OnRemoteMd5Got(std::function<void(Md5Map)> cb)
		{
			m_remoteMd5Got = cb;
		}

		void OnLocalMd5Got(std::function<void(Md5Map)> cb)
		{
			m_localMd5Got = cb;
		}

		void OnFileCreate(std::function<void(std::string)> cb)
		{
			m_fileCreate = cb;
		}

		void OnFileDelete(std::function<void(std::string)> cb)
		{
			m_fileDelete = cb;
		}

		void OnFolderCreate(std::function<void(std::string)> cb)
		{
			m_folderDelete = cb;
		}

		void OnDifferencesGot(std::function<void(std::vector<std::pair<std::string, int>>)> cb)
		{
			m_differencesGot = cb;
		}
#pragma endregion 


	public:
		FileClient(const char* url, const char* remoteMd5Path, const char* localPath);
		void sync(std::function<void(bool, const std::string&)> cb);
		void parse_remote_md5(const std::string& cs);

	private:
		void download(const std::vector<std::pair<std::string, int>>& diff);
		void download(std::vector<std::string>& files);

		void remove_expired_files();
		void remove_empty_directories(const std::string& root);

		void clear();
		void scan_local_md5();
		std::vector<std::pair<std::string, int>> make_diff();

	private:
		std::string m_url;
		std::string m_remoteMd5Path;
		std::string m_localPath;

		Md5Map m_localMd5Map;
		Md5Map m_remoteMd5Map;
	};
}

#endif



