
(function(JACERE) {
	
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

	JACERE.FileStream = function(file) {
		this.file = file;
		this.reader = new FileReaderSync();
	};
	
	JACERE.FileStream.prototype = {

		constructor: JACERE.FileStream,
		
		read: function(start, end, chunkSize, progressCallback) {
			var length = (end - start);
			if (progressCallback && length > chunkSize) {
				var buffer = new JACERE.BufferWrapper(length);
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
		}
		
	};

	JACERE.HttpStream = function(url) {
		this.url = url;
	};
	
	JACERE.HttpStream.prototype = {

		constructor: JACERE.HttpStream,
		
		read: function(start, end) {
			var xhr = new XMLHttpRequest();
			xhr.open('GET', this.url, false);
			// should this be (end - 1)?
			xhr.setRequestHeader('Range', String.format('bytes={0}-{1}', start, end));
			xhr.responseType = 'arraybuffer';
			xhr.send(null);

			return xhr.response;
		}
		
	};
	
}(self.JACERE = self.JACERE || {}));