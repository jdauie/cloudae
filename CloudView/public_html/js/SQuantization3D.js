
function SQuantization3D(reader) {
	this.scale = reader.readVector3();
	this.offset = reader.readVector3();
	
	this.convert = function(extent) {
		return new THREE.Box3(
			extent.min.sub(this.offset).divide(this.scale),
			extent.max.sub(this.offset).divide(this.scale)
		);
	};
}