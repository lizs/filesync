
#include <boost/property_tree/ptree.hpp>
#include <boost/property_tree/json_parser.hpp>
#include <boost/filesystem.hpp>
#include <fstream>
#include <sstream>
#include <iostream>

#include "json/document.h"
#include "json/writer.h"
#include "json/stringbuffer.h"

using namespace  rapidjson;
using namespace boost;
int main() 
{
	try 
	{
		property_tree::ptree tree;
		property_tree::read_json(filesystem::current_path().string() + "/test.json", tree);

		for (auto & kv : tree.get_child("root")) 
		{
			auto node = kv.second;
			auto path = node.get<std::string>("path");
			auto size = node.get<int>("size");
			auto md5 = node.get<std::string>("md5");

			std::cout << "path : " << path << "\t size:" << size << "\t md5" << md5 << std::endl;
		}
	}
	catch (std::exception & e)
	{
		std::cout << e.what() << std::endl;
	}

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
}