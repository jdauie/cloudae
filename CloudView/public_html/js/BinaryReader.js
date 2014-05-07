function BinaryReader(arrayBuffer, offset) {
	if (typeof offset === 'undefined') offset = 0;
	
	this.littleEndian = true;
	this.buffer = arrayBuffer;
	this.view = new DataView(arrayBuffer, offset);
	this.offset = offset;
	this.position = 0;
}

ArrayBuffer.prototype.readObject = function(name, namespace) {
	var br = new BinaryReader(this);
	return br.readObject(name, namespace);
};

BinaryReader.prototype.skip = function(count) {
	return this.position += count;
};

BinaryReader.prototype.seek = function(position) {
	return this.position = position;
};

BinaryReader.prototype.readObject = function(name, namespace) {
	if (!namespace)
		namespace = self;
	//console.log("readObject: "+namespace+"."+name);
	return new namespace[name](this);
};

BinaryReader.prototype.readVector3 = function() {
	return new THREE.Vector3(this.readFloat64(), this.readFloat64(), this.readFloat64());
};

BinaryReader.prototype.readBox3 = function() {
	return new THREE.Box3(this.readVector3(), this.readVector3());
};

BinaryReader.prototype.readArrayBuffer = function(size) {
	var offset = this.offset + this.position;
	this.position += size;
	return this.buffer.slice(offset, offset + size);
};

BinaryReader.prototype.readUint64 = function() {
	var high = this.view.getUint32(this.position, this.littleEndian);
	this.position += 4;
	var low = this.view.getUint32(this.position, this.littleEndian);
	this.position += 4;
	return (high << 32) | low;
};

BinaryReader.prototype.readUint64Array = function(size) {
	var array = new Array();
	for (var i = 0; i < size; i++) {
		array.push(this.readUint64());
	}
	return array;
};

BinaryReader.prototype.readUint32Array = function(size) {
	var array = new Array();
	for (var i = 0; i < size; i++) {
		array.push(this.readUint32());
	}
	return array;
};

BinaryReader.prototype.readAsciiString = function(size) {
	var offset = this.offset + this.position;
	this.position += size;
	return String.fromCharCode.apply(null, new Uint8Array(this.buffer, offset, size)).replace(/\0/g, '');
};


BinaryReader.prototype.readInt8 = function() {
	return this.view.getInt8(this.position++, this.littleEndian);
};

BinaryReader.prototype.readUint8 = function() {
	return this.view.getUint8(this.position++, this.littleEndian);
};

BinaryReader.prototype.readInt16 = function() {
	var value = this.view.getInt16(this.position, this.littleEndian);
	this.position += 2;
	return value;
};

BinaryReader.prototype.readUint16 = function() {
	var value = this.view.getUint16(this.position, this.littleEndian);
	this.position += 2;
	return value;
};

BinaryReader.prototype.readInt32 = function() {
	var value = this.view.getInt32(this.position, this.littleEndian);
	this.position += 4;
	return value;
};

BinaryReader.prototype.readUint32 = function() {
	var value = this.view.getUint32(this.position, this.littleEndian);
	this.position += 4;
	return value;
};

BinaryReader.prototype.readFloat32 = function() {
	var value = this.view.getFloat32(this.position, this.littleEndian);
	this.position += 4;
	return value;
};

BinaryReader.prototype.readFloat64 = function() {
	var value = this.view.getFloat64(this.position, this.littleEndian);
	this.position += 8;
	return value;
};
