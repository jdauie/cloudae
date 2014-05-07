
function LASInfo(file) {
	this.file = file;
	this.startTime = Date.now();
	
	this.settings = {
		loader: clone(settings.loader),
		render: clone(settings.render)
	};
	
	this.setHeader = function(header, chunks, stats) {
		this.header = header;
		this.chunks = chunks;
		this.stats = stats;
		this.step = Math.ceil(header.numberOfPointRecords / Math.min(this.settings.loader.maxPoints, header.numberOfPointRecords));
	};
	
	this.getPointReader = function(buffer) {
		return new PointReader(this, buffer);
	};
}

function PointReader(info, buffer) {
	this.reader = new BinaryReader(buffer);
	this.points = (buffer.byteLength / info.header.pointDataRecordLength);
	this.filteredPoints = ~~Math.ceil(this.points / info.step);
	
	this.currentPointIndex = 0;

	this.readPoint = function() {
		if (this.currentPointIndex >= this.points) {
			return null;
		}
		
		this.reader.seek(this.currentPointIndex * info.header.pointDataRecordLength);
		var point = this.reader.readUnquantizedPoint3D(info.header.quantization);
		
		this.currentPointIndex += info.step;
		
		return point;
	};
}