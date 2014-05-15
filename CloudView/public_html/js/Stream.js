
function FileStream(file) {
	this.file = file;
	this.reader = new FileReaderSync();
	
	this.read = function(start, end) {
		var slice = this.file.slice(start, end);
		var buffer = this.reader.readAsArrayBuffer(slice);
		return buffer;
	};
}

function HttpStream(url) {
	this.url = url;
	
	this.read = function(start, end) {
		var xhr = new XMLHttpRequest();
		xhr.open('GET', this.url, false);
		xhr.setRequestHeader('Range', String.format('bytes={0}-{1}', start, end));
		xhr.responseType = 'arraybuffer';
		xhr.send(null);
		
		return xhr.response;
	};
}