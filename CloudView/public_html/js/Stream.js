
(function(JACERE) {
	
	/*JACERE.PointCloudBinarySourceChunk = function() {
		
	};
	
	JACERE.ChunkReader = function(start, end, chunkSize) {
		
	};
	
	FileReaderSync.prototype.readChunk = function(chunk) {
		var slice = this.file.slice(chunk.start, chunk.end);
		var buffer = this.reader.readAsArrayBuffer(slice);
		
		return {
			
		};
	};*/
	
	JACERE.BufferWrapper = function(length) {
		this.buffer = new ArrayBuffer(length);
		this.bufferView = new Uint8Array(this.buffer);
		this.index = 0;
	};
	
	JACERE.BufferWrapper.prototype = {

		constructor: JACERE.BufferWrapper,
		
		append: function(data) {
			var dataView = new Uint8Array(data);
			this.bufferView.set(dataView, this.index);
			this.index += data.byteLength;
		},

		complete: function() {
			return (this.index === this.buffer.byteLength);
		},

		progress: function() {
			return (this.index / this.buffer.byteLength);
		}
		
	};
	
	JACERE.Stream = function() {
		//
	};

	JACERE.Stream.prototype = {
		
		constructor: JACERE.Stream,
		
		read: function(start, end, chunkSize, progressCallback) {
			var length = (end - start);
			if (progressCallback && length > chunkSize) {
				var wrapper = new JACERE.BufferWrapper(length);
				while (!wrapper.complete()) {
					var sliceStart = start + wrapper.index;
					var sliceEnd = Math.min(end, sliceStart + chunkSize);
					var chunk = this.readChunk(sliceStart, sliceEnd);
					wrapper.append(chunk);
					progressCallback(wrapper.progress());
				}
				return wrapper.buffer;
			}
			else {
				return this.readChunk(start, end);
			}
		}
	};

	JACERE.FileStream = function(file) {
		JACERE.Stream.call(this);
		
		this.file = file;
		this.reader = new FileReaderSync();
		this.length = file.size;
	};
	
	JACERE.FileStream.prototype = Object.create(JACERE.Stream.prototype);
	
	JACERE.FileStream.prototype.readChunk = function(start, end) {
		var slice = this.file.slice(start, end);
		var buffer = this.reader.readAsArrayBuffer(slice);
		return buffer;
	};

	JACERE.HttpStream = function(url) {
		JACERE.Stream.call(this);
		
		this.url = url;
		this.length = this.getLength();
	};
	
	JACERE.HttpStream.prototype = Object.create(JACERE.Stream.prototype);
	
	JACERE.HttpStream.prototype.readChunk = function(start, end) {
		var xhr = new XMLHttpRequest();
		xhr.open('GET', this.url, false);
		// end byte is inclusive here, unlike file reader
		xhr.setRequestHeader('Range', String.format('bytes={0}-{1}', start, end - 1));
		xhr.responseType = 'arraybuffer';
		xhr.send(null);
		return xhr.response;
	};
	
	JACERE.HttpStream.prototype.getLength = function() {
		var xhr = new XMLHttpRequest();
		xhr.open('HEAD', this.url, false);
		xhr.send(null);
		return xhr.getResponseHeader('Content-Length');
	};
	
}(self.JACERE = self.JACERE || {}));