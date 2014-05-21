
function BufferWrapper(length) {
	this.buffer = new ArrayBuffer(length);
	this.bufferView = new Uint8Array(this.buffer);
	this.index = 0;
	
	this.append = function(data) {
		var dataView = new Uint8Array(data);
		this.bufferView.set(dataView, this.index);
		this.index += data.byteLength;
	};
	
	this.complete = function() {
		return (this.index === this.buffer.byteLength);
	};
	
	this.progress = function() {
		return (this.index / this.buffer.byteLength);
	};
}

function FileStream(file) {
	this.file = file;
	this.reader = new FileReaderSync();
	
	this.read = function(start, end, chunkSize, progressCallback) {
		var length = (end - start);
		if (progressCallback && length > chunkSize) {
			var buffer = new BufferWrapper(length);
			while (!buffer.complete()) {
				var slice = this.file.slice(start + buffer.index, (Math.min(end, start + buffer.index + chunkSize)));
				var chunk = this.reader.readAsArrayBuffer(slice);
				buffer.append(chunk);
				progressCallback(buffer.progress());
			}
			return buffer.buffer;
		}
		else {
			var slice = this.file.slice(start, end);
			var buffer = this.reader.readAsArrayBuffer(slice);
			return buffer;
		}
	};
}

function HttpStream(url) {
	this.url = url;
	
	this.read = function(start, end) {
		var xhr = new XMLHttpRequest();
		xhr.open('GET', this.url, false);
		// should this be (end - 1)?
		xhr.setRequestHeader('Range', String.format('bytes={0}-{1}', start, end));
		xhr.responseType = 'arraybuffer';
		xhr.send(null);
		
		return xhr.response;
	};
}