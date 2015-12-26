var static = require('node-static');
var chokidar = require('chokidar');
var md5 = require('md5');
var fs = require('fs');
var dispatch = require('./dispatcher.js');
var http = require('http');
var log = console.log.bind(console);
var fileServer = new static.Server('./public');

var md5json = {};

var md5_handle = function(req, res){
    res.end(JSON.stringify(md5json));
};

var file_server_handle = function(req, res)
{            
    req.addListener('end', function ()
    {
        fileServer.serve(req, res, function (err, result)
        {
            if (err)
            { // There was an error serving the file
                console.error("Error serving " + req.url + " - " + err.message);

                // Respond to the client
                res.writeHead(err.status, err.headers);
                res.end();
            }
        });
    }).resume();
};

var server = http.createServer(
	dispatch(
	{
        '/md5': (req, res)=>md5_handle(req, res),
        '/(.+)': (req, res)=>file_server_handle(req, res),
    }, '', function(){
    	log('next invoked!');
    })
);

server.listen(8080);

// One-liner for current directory, ignores .dotfiles
var watcher = chokidar.watch('./public', {
  ignored: /[\/\\]\./,
  persistent: true
});

var getmd5 = function(path, cb){
	fs.readFile(path, function(err, buf) {
  		cb(err ? '' : md5(buf), err);
	});
}

var remove_root = function(path){
	path = path.replace('public\\', '');
	return path;
}

watcher
  .on('add', path => getmd5(path, (code, err)=>{
  		md5json[remove_root(path)] = code;
	  	log(path, ' created');
	}))
  .on('change', path => getmd5(path, (code, err)=>{
  		md5json[remove_root(path)] = code;
	  	log(path, ' changed');
	}))
  .on('unlink', path =>{
	  	delete md5json[remove_root(path)];
	  	log(path, ' removed');
	})
  .on('error', error => log(`Watcher error: ${error}`))
  .on('ready', () => {
	  	log('Initial scan complete. Ready for changes');
	});