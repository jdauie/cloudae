importScripts(
	"libs/three.js/three.min.js",
	'util.js',
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

function FileSource(file, reader, header, chunkSize) {
	this.file = file;
	this.reader = reader;
	this.header = header;
	this.chunkSizeMax = chunkSize;
}

function loadPoints(offset, count, step) {
	
	var filteredCount = count;
	var start = current.header.offsetToPointData + (offset * current.header.pointDataRecordLength);
	var end = start + (count * current.header.pointDataRecordLength);
	
	var buffer;
	
	if (step === 1) {
		// read all the points at once
		// maybe break it up for progress reasons
		var slice = current.file.slice(start, end);
		buffer = current.reader.readAsArrayBuffer(slice);
	}
	else {
		// calculate chunks
		var chunkPoints = ~~Math.floor(current.chunkSizeMax / current.header.pointDataRecordLength);
		var chunkBytes = chunkPoints * current.header.pointDataRecordLength;
		var chunks = Math.ceil(current.header.numberOfPointRecords / chunkPoints);
		
		// allocate adequate space for thinned points
		var filteredPointsPerChunk = ~~Math.ceil(chunkPoints / step);
		var filterStep = (step * current.header.pointDataRecordLength);
		filteredCount = (filteredPointsPerChunk * chunks);
		buffer = new ArrayBuffer(filteredCount * current.header.pointDataRecordLength);
		var bufferView = new Uint8Array(buffer);
		var bufferIndex = 0;
		
		// read file, copy thinned points, and report progress
		var chunkStart = start;
		var chunkEnd = start + chunkBytes;
		for (var i = 0; i < chunks; i++, chunkStart += chunkBytes, chunkEnd += chunkBytes) {
			if (chunkEnd > end)
				chunkEnd = end;
			
			var slice = current.file.slice(chunkStart, chunkEnd);
			var chunkBuffer = current.reader.readAsArrayBuffer(slice);
			var chunkBufferView = new Uint8Array(chunkBuffer);
			
			// filter points
			var chunkSize = (chunkEnd - chunkStart);
			for (var chunkIndex = 0; chunkIndex < chunkSize; chunkIndex += filterStep, bufferIndex += current.header.pointDataRecordLength) {
				// copy point
				var pointView = chunkBufferView.subarray(chunkIndex, chunkIndex + current.header.pointDataRecordLength);
				bufferView.set(pointView, bufferIndex);
			}
			
			self.postMessage({
				progress: (i + 1) / chunks
			});
		}
		
		filteredCount = (bufferIndex / current.header.pointDataRecordLength);
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
	var reader = new FileReaderSync();
	var buffer = reader.readAsArrayBuffer(file.slice(0, LAS_MAX_SUPPORTED_HEADER_SIZE));
	var header = buffer.readObject("LASHeader");
	
	current = new FileSource(file, reader, header, chunkSize);
	
	// read evlrs to find known records
	var evlrPosition = header.startOfFirstExtendedVariableLengthRecord;
	var evlrSizeBeforeData = 60;
	var bufferTiles = new ArrayBuffer(0);
	var bufferStats = new ArrayBuffer(0);
	for (var i = 0; i < header.numberOfExtendedVariableLengthRecords; i++) {
		var buffer2 = reader.readAsArrayBuffer(file.slice(evlrPosition, evlrPosition + evlrSizeBeforeData));
		var evlr = buffer2.readObject("LASEVLR");
		var evlrPositionNext = evlrPosition + evlrSizeBeforeData + evlr.recordLengthAfterHeader;
		if (evlr.userID === "Jacere") {
			buffer2 = reader.readAsArrayBuffer(file.slice(evlrPosition + evlrSizeBeforeData, evlrPositionNext));
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

