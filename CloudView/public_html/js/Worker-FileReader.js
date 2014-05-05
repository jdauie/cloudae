importScripts(
	"libs/three.js/three.min.js",
	'BinaryReader.js',
	'LASHeader.js'
);

self.addEventListener('message', function(e) {
  var data = e.data;
  loadFile(data.file);
}, false);

function loadFile(file) {
	var reader = new FileReaderSync();
	
	var arraybuffer = reader.readAsArrayBuffer(file.slice(0, LAS_MAX_SUPPORTED_HEADER_SIZE));
	var header = arraybuffer.readObject("LASHeader");
	
	var chunkBytes = 2*1024*1024;
	var chunkPoints = ~~(chunkBytes / header.pointDataRecordLength);
	chunkBytes = chunkPoints * header.pointDataRecordLength;
	var chunks = Math.ceil(header.numberOfPointRecords / chunkPoints);
	
	// debug
	//chunks = 1;
	
	self.postMessage({
		header: arraybuffer,
		chunks: chunks
	}, [arraybuffer]);
	
	for (var i = 0; i < chunks; i++) {
		
		var start = header.offsetToPointData + (i * chunkBytes);
		var end = start + chunkBytes;
		var points = chunkPoints;
		if (end > file.size) {
			end = file.size;
			points = ((end - start) / header.pointDataRecordLength);
		}
		
		var slice = file.slice(start, end);
		arraybuffer = reader.readAsArrayBuffer(slice);
		self.postMessage({
			chunk: arraybuffer,
			index: i,
			points: points,
			pointSize: header.pointDataRecordLength
		}, [arraybuffer]);
	}
}

