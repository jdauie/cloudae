importScripts(
	"libs/three.js/three.min.js",
	'reader.js',
	'las.js'
);

self.addEventListener('message', function(e) {
  var data = e.data;
  loadFile(data.file);
}, false);

function loadFile2(file) {
	var reader = new FileReaderSync();
	//reader.onprogress = updateProgress;
	
	var arraybuffer = reader.readAsArrayBuffer(file);
	self.postMessage(arraybuffer, [arraybuffer]);
}

function updateProgress(evt) {
	if (evt.lengthComputable) {
		var percentComplete = (evt.loaded / evt.total) * 100;
		self.postMessage({progress: ~~percentComplete});
	}
}

function loadFile(file) {
	var reader = new FileReaderSync();
	
	// read header
	var arraybuffer = reader.readAsArrayBuffer(file.slice(0, LAS_MAX_SUPPORTED_HEADER_SIZE));
	var br = new BinaryReader(arraybuffer, 0, true);
	
	var header = br.readObject("LASHeader");
	self.postMessage({
		header: arraybuffer
	}, [arraybuffer]);
	
	var chunkBytes = 2*1024*1024;
	var chunkPoints = ~~(chunkBytes / header.pointDataRecordLength);
	chunkBytes = chunkPoints * header.pointDataRecordLength;
	var chunks = Math.ceil(header.numberOfPointRecords / chunkPoints);
	
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

