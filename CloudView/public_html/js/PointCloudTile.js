
(function(JACERE) {
	
	JACERE.PointCloudTile = function(tileSet, col, row, validIndex, offset, count, lowResOffset, lowResCount) {
		this.tileSet = tileSet;

		this.row = row;
		this.col = col;
		this.pointCount = count;
		this.lowResOffset = lowResOffset;
		this.lowResCount = lowResCount;

		this.pointOffset = offset;
		this.validIndex = validIndex;
	};

	JACERE.PointCloudTile.prototype = {
		
		constructor: JACERE.PointCloudTile,
		
		toString: function() {
			return String.format('[{0}, {1}]', this.row, this.col);
		}
	};
	
}(self.JACERE = self.JACERE || {}));