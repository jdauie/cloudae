
function Statistics(reader) {
	this.mean = reader.readFloat64();
	this.variance = reader.readFloat64();
	this.stdDev = Math.sqrt(this.variance);
	this.modeApproximate = reader.readFloat64();
}