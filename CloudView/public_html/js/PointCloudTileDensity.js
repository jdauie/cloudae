
(function(JACERE) {
	
	JACERE.PointCloudTileDensity = function(reader) {
		this.pointCount        = reader.readUint64();
		this.tileCount         = reader.readInt32();
		this.validTileCount    = reader.readInt32();

		this.minTileCount      = reader.readInt32();
		this.maxTileCount      = reader.readInt32();
		this.medianTileCount   = reader.readInt32();
		this.meanTileCount     = reader.readInt32();

		this.minTileDensity    = reader.readFloat64();
		this.maxTileDensity    = reader.readFloat64();
		this.medianTileDensity = reader.readFloat64();
		this.meanTileDensity   = reader.readFloat64();
	};
	
}(self.JACERE = self.JACERE || {}));