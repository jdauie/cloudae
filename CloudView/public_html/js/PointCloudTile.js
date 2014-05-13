
function PointCloudTile(tileSet, col, row, validIndex, offset, count, lowResOffset, lowResCount) {
	this.tileSet = tileSet;

	this.row = row;
	this.col = col;
	this.pointCount = count;
	this.lowResOffset = lowResOffset;
	this.lowResCount = lowResCount;

	this.pointOffset = offset;
	this.validIndex = validIndex;
}