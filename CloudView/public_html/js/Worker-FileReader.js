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
	else {
		var step = (e.data.step ? e.data.step : 1);
		loadPoints(e.data.pointOffset, e.data.pointCount, step);
	}
}, false);

function PointBufferWrapper(buffer, recordLength) {
	this.buffer = buffer;
	this.bufferView = new Uint8Array(buffer);
	this.recordLength = recordLength;
	this.index = 0;

	this.append = function(view, offset) {
		var end = offset + this.recordLength;
		for (var i = offset; i < end; i++) {
			this.bufferView[this.index++] = view[i];
		}
	};

	this.count = function() {
		return (this.index / this.recordLength);
	};

	this.progress = function() {
		return (this.index / this.buffer.byteLength);
	};
}

function FileSource(stream, header, chunkSize) {
	this.stream = stream;
	this.header = header;
	this.chunkSizeMax = chunkSize;
	this.chunkPoints = Math.floor(this.chunkSizeMax / this.header.pointDataRecordLength);
	this.chunkSize = this.chunkPoints * this.header.pointDataRecordLength;
}

function createReadStream(file) {
	if (file.constructor.name === "File") {
		return new JACERE.FileStream(file);
	}
	else {
		return new JACERE.HttpStream(file);
	}
}

function updateProgress(ratio) {
	self.postMessage({
		progress: ratio
	});
}

function chunk(start, end) {
	this.start = start;
	this.end = end;
}

function loadPoints(offset, count, step) {

	var pointLength = current.header.pointDataRecordLength;

	var filteredCount = count;
	var start = current.header.offsetToPointData + (offset * pointLength);
	var end = start + (count * pointLength);

	var buffer;

	if (step === 1) {
		buffer = current.stream.read(start, end, current.chunkSize, updateProgress);
	}
	else {
		var chunks = Math.ceil(count / current.chunkPoints);

		// allocate adequate space for thinned points
		var filteredPointsPerChunk = ~~Math.ceil(current.chunkPoints / step);
		var filterStep = (step * pointLength);
		filteredCount = (filteredPointsPerChunk * chunks);
		buffer = new ArrayBuffer(filteredCount * pointLength);
		var wrapper = new PointBufferWrapper(buffer, pointLength);

		// read file, copy thinned points, and report progress
		var chunkStart = start;
		var chunkEnd = start + current.chunkSize;
		for (var i = 0; i < chunks; i++, chunkStart += current.chunkSize, chunkEnd += current.chunkSize) {
			if (chunkEnd > end)
				chunkEnd = end;

			var chunkBuffer = current.stream.read(chunkStart, chunkEnd);
			var chunkBufferView = new Uint8Array(chunkBuffer);

			// copy filtered points
			var chunkSize = (chunkEnd - chunkStart);
			for (var chunkIndex = 0; chunkIndex < chunkSize; chunkIndex += filterStep) {
				wrapper.append(chunkBufferView, chunkIndex);
			}

			updateProgress(wrapper.progress());
		}

		filteredCount = wrapper.count();
	}

	self.postMessage({
		buffer: buffer,
		pointCount: filteredCount,
		// debugging?
		request: {
			pointOffset: offset,
			pointCount: count,
			step: step
		}
	}, [buffer]);
}

function loadHeader(file, chunkSize) {
	var stream = createReadStream(file);

	var buffer = stream.read(0, JACERE.LASHeader.MAX_SUPPORTED_HEADER_SIZE);
	var header = buffer.readObject("LASHeader", JACERE);

	current = new FileSource(stream, header, chunkSize);

	var records = current.header.readEVLRs(stream, {
		tiles: new JACERE.LASRecordIdentifier("Jacere", 0),
		stats: new JACERE.LASRecordIdentifier("Jacere", 1)
	});

	var bufferTiles = records.tiles || new ArrayBuffer(0);
	var bufferStats = records.stats || new ArrayBuffer(0);

	self.postMessage({
		header: buffer,
		tiles: bufferTiles,
		stats: bufferStats,
		length: stream.length
	}, [buffer, bufferTiles, bufferStats]);
}