importScripts(
	"libs/three.js/three.min.js",
	'util.js',
	'Stream.js',
	'BinaryReader.js',
	'SQuantization3D.js',
	'LASHeader.js'
);

var current = null;

self.addEventListener('message', function(e) {
	var data = e.data;
	if (e.data.file) {
		loadHeader(data.file, data.chunkSize);
	}
	else if (e.data.pointCount) {
		var step = (e.data.step ? e.data.step : 1);
		loadPoints(e.data.pointOffset, e.data.pointCount, step);
	}
}, false);

function FileSource(stream, header, chunkSize) {
	this.stream = stream;
	this.header = header;
	this.chunkSizeMax = chunkSize;
}

function createReadStream(file) {
	if (file.constructor.name === "File") {
		return new FileStream(file);
	}
	else {
		return new HttpStream(file);
	}
}

function loadPoints(offset, count, step) {
	
	var pointOffset = current.header.offsetToPointData;
	var pointCount = current.header.numberOfPointRecords;
	var pointLength = current.header.pointDataRecordLength;
	
	var filteredCount = count;
	var start = pointOffset + (offset * pointLength);
	var end = start + (count * pointLength);
	
	var buffer;
	
	if (step === 1) {
		// read all the points at once (maybe break it up for progress reasons?)
		buffer = current.stream.read(start, end);
	}
	else {
		// calculate chunks
		var chunkPoints = ~~Math.floor(current.chunkSizeMax / pointLength);
		var chunkBytes = chunkPoints * pointLength;
		var chunks = Math.ceil(pointCount / chunkPoints);
		
		// allocate adequate space for thinned points
		var filteredPointsPerChunk = ~~Math.ceil(chunkPoints / step);
		var filterStep = (step * pointLength);
		filteredCount = (filteredPointsPerChunk * chunks);
		buffer = new ArrayBuffer(filteredCount * pointLength);
		var bufferView = new Uint8Array(buffer);
		var bufferIndex = 0;
		
		// read file, copy thinned points, and report progress
		var chunkStart = start;
		var chunkEnd = start + chunkBytes;
		for (var i = 0; i < chunks; i++, chunkStart += chunkBytes, chunkEnd += chunkBytes) {
			if (chunkEnd > end)
				chunkEnd = end;
			
			var chunkBuffer = current.stream.read(chunkStart, chunkEnd);
			var chunkBufferView = new Uint8Array(chunkBuffer);
			
			// filter points
			var chunkSize = (chunkEnd - chunkStart);
			for (var chunkIndex = 0; chunkIndex < chunkSize; chunkIndex += filterStep, bufferIndex += pointLength) {
				// copy point
				var pointView = chunkBufferView.subarray(chunkIndex, chunkIndex + pointLength);
				bufferView.set(pointView, bufferIndex);
			}
			
			self.postMessage({
				progress: (i + 1) / chunks
			});
		}
		
		filteredCount = (bufferIndex / pointLength);
	}
	
	self.postMessage({
		buffer: buffer,
		pointOffset: offset,
		pointCount: count,
		filteredCount: filteredCount,
		step: step
	}, [buffer]);
}

function loadHeader(file, chunkSize) {
	var stream = createReadStream(file);
	
	var buffer = stream.read(0, LAS_MAX_SUPPORTED_HEADER_SIZE);
	var header = buffer.readObject("LASHeader");
	
	current = new FileSource(stream, header, chunkSize);
	
	// read evlrs to find known records
	var evlrPosition = header.startOfFirstExtendedVariableLengthRecord;
	var evlrSizeBeforeData = 60;
	var bufferTiles = new ArrayBuffer(0);
	var bufferStats = new ArrayBuffer(0);
	for (var i = 0; i < header.numberOfExtendedVariableLengthRecords; i++) {
		var buffer2 = stream.read(evlrPosition, evlrPosition + evlrSizeBeforeData);
		var evlr = buffer2.readObject("LASEVLR");
		var evlrPositionNext = evlrPosition + evlrSizeBeforeData + evlr.recordLengthAfterHeader;
		if (evlr.userID === "Jacere") {
			buffer2 = stream.read(evlrPosition + evlrSizeBeforeData, evlrPositionNext);
			if (evlr.recordID === 0) {
				bufferTiles = buffer2;
			}
			else if (evlr.recordID === 1) {
				bufferStats = buffer2;
			}
		}
		evlrPosition = evlrPositionNext;
	}
	
	self.postMessage({
		header: buffer,
		tiles: bufferTiles,
		stats: bufferStats
	}, [buffer, bufferTiles, bufferStats]);
}

