
(function(JACERE) {
	
	JACERE.SQuantization3D = function(reader) {
		this.scale = reader.readVector3();
		this.offset = reader.readVector3();
	};

	JACERE.SQuantization3D.prototype = {
		
		constructor: JACERE.SQuantization3D,
		
		convert: function(extent) {
			return new THREE.Box3(
				extent.min.sub(this.offset).divide(this.scale),
				extent.max.sub(this.offset).divide(this.scale)
			);
		}
	};
	
}(self.JACERE = self.JACERE || {}));