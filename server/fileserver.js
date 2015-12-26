var static = require('node-static');
var chokidar = require('chokidar');
var md5 = require('md5');
var fs = require('fs');
var dispatch = require('./dispatcher.js');
var http = require('http');
var log = console.log.bind(console);
var fileServer = new static.Server('./public');

var md5json = {};

var server = http.createServer(dispatch(
{
    '/md5': function(req, res)
    {
        res.end(JSON.stringify(md5json));
    },

    '/public/(\\w+.\\w+)': file_server_handle
   }, 
   'res', 
   function(){
    log("next invoked!");
}));

server.listen(8080);

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
  }
}


// One-liner for current directory, ignores .dotfiles
var watcher = chokidar.watch('./public', 
{
  ignored: /[\/\\]\./,
  persistent: true
});

var getmd5 = function(path, cb)
{
	fs.readFile(path, function(err, buf) 
    {
        cb(err ? '' : md5(buf), err);

        log("md5json prints below:")
        for (var i in md5json) {
		    if (!md5json.hasOwnProperty(i)) continue; // safety!
		    log(md5json[i])
		}
	});
}

watcher
.on('add', path => getmd5(path, (code, err)=> md5json[path] = code))
.on('change', path => getmd5(path, (code, err)=> md5json[path] = code))
.on('unlink', path => 
{
    delete md5json[path];
    log(path, ' removed');
})
.on('error', error => log(`Watcher error: ${error}`))
.on('ready', () => {
    log('Initial scan complete. Ready for changes');
});