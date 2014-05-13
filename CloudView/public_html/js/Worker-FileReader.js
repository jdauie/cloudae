importScripts(
	"libs/three.js/three.min.js",
	'util.js',
	'BinaryReader.js',
	'SQuantization3D.js',
	'LASHeader.js'
);

self.addEventListener('message', function(e) {
	var data = e.data;
	if (e.data.file) {
		loadHeader(data.file, data.chunkSize);
	}
	else if (e.data.pointOffset) {
		loadPoints(e.data.pointOffset, e.data.pointCount, e.data.id);
	}
}, false);

var source;
var reader;
var header;
var tiles;
var stats;

function loadPoints(offset, count, id) {
	var start = header.offsetToPointData + (offset * header.pointDataRecordLength);
	var end = start + (count * header.pointDataRecordLength);
	
	var slice = source.slice(start, end);
	var buffer = reader.readAsArrayBuffer(slice);
	self.postMessage({
		chunk: buffer,
		pointOffset: offset,
		pointCount: count,
		id: id
	}, [buffer]);
}

function loadHeader(file, chunkBytes) {
	source = file;
	reader = new FileReaderSync();
	
	var buffer = reader.readAsArrayBuffer(file.slice(0, LAS_MAX_SUPPORTED_HEADER_SIZE));
	header = buffer.readObject("LASHeader");
	
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
	
	var chunkPoints = ~~Math.floor(chunkBytes / header.pointDataRecordLength);
	chunkBytes = chunkPoints * header.pointDataRecordLength;
	var chunks = Math.ceil(header.numberOfPointRecords / chunkPoints);
	
	// debug
	//chunks = 1;
	
	self.postMessage({
		header: buffer,
		chunks: chunks,
		tiles: bufferTiles,
		zstats: bufferStats
	}, [buffer, bufferTiles, bufferStats]);
	
	// debug
	return;
	
	var endOfPointData = header.offsetToPointData + (header.numberOfPointRecords * header.pointDataRecordLength);
	
	// jump to low-res
	
	
	for (var i = 0; i < chunks; i++) {
		
		var start = header.offsetToPointData + (i * chunkBytes);
		var end = start + chunkBytes;
		if (end > endOfPointData) {
			end = endOfPointData;
		}
		
		var slice = file.slice(start, end);
		buffer = reader.readAsArrayBuffer(slice);
		self.postMessage({
			chunk: buffer,
			index: i
		}, [buffer]);
	}
}

