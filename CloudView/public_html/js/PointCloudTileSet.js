
(function(JACERE) {
	
	JACERE.PointCloudTileSet = function(reader) {
		this.rows = reader.readUint16();
		this.cols = reader.readUint16();
		this.tileSizeY = reader.readInt32();
		this.tileSizeX = reader.readInt32();

		this.tileCount = this.rows * this.cols;

		this.extent = reader.readBox3();
		this.quantization = reader.readObject("SQuantization3D", JACERE);
		this.quantizedExtent = this.quantization.convert(this.extent);
		this.density = reader.readObject("PointCloudTileDensity", JACERE);

		this.pointCount = this.density.pointCount;
		this.validTileCount = this.density.validTileCount;

		this.lowResCount = 0;

		//this.tileIndex = [];
		this.tiles = [];

		// fill in valid tiles (dense)
		var pointOffset = 0;
		var i = 0;
		for (var y = 0; y < this.rows; y++) {
			for (var x = 0; x < this.cols; x++) {
				var pointCount = reader.readInt32();
				var lowResCount = reader.readInt32();
				if (pointCount > 0)
				{
					this.tiles.push(new JACERE.PointCloudTile(this, x, y, i, pointOffset, pointCount, this.lowResCount, lowResCount));
					//this.tileIndex.Add(tile.Index, i);

					pointOffset += (pointCount - lowResCount);
					this.lowResCount += lowResCount;
					++i;
				}
			}
		}

		this.lowResOffset = this.pointCount - this.lowResCount;
	};

	JACERE.PointCloudTileSet.prototype = {
		
		constructor: JACERE.PointCloudTileSet,
		
		getValidTile: function(validIndex) {
			if (validIndex < this.validTileCount) {
				return this.tiles[validIndex];
			}
		},
		
		getTile: function(y, x) {
			for (var i = 0; i < this.validTileCount; i++) {
				var tile = this.tiles[i];
				if (tile.row === y && tile.col === x) {
					return tile;
				}
			}
			return null;
		}
	};
	
}(self.JACERE = self.JACERE || {}));