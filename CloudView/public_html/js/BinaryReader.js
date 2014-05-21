
(function(JACERE) {
	
	ArrayBuffer.prototype.readObject = function(name, namespace) {
		var br = new JACERE.BinaryReader(this);
		return br.readObject(name, namespace);
	};
	
	JACERE.BinaryReader = function(arrayBuffer, offset) {
		this.littleEndian = true;
		this.buffer = arrayBuffer;
		this.view = new DataView(arrayBuffer, offset);
		this.offset = offset || 0;
		this.position = 0;
	};

	JACERE.BinaryReader.prototype = {

		constructor: JACERE.BinaryReader,

		skip: function(count) {
			return this.position += count;
		},

		seek: function(position) {
			return this.position = position;
		},

		readObject: function(name, namespace) {
			if (!namespace)
				namespace = self;
			//console.log("readObject: "+namespace+"."+name);
			return new namespace[name](this);
		},

		readVector3: function() {
			return new THREE.Vector3(this.readFloat64(), this.readFloat64(), this.readFloat64());
		},

		readBox3: function() {
			return new THREE.Box3(this.readVector3(), this.readVector3());
		},

		readArrayBuffer: function(size) {
			var offset = this.offset + this.position;
			this.position += size;
			return this.buffer.slice(offset, offset + size);
		},

		readUint64: function() {
			var low = this.view.getUint32(this.position, this.littleEndian);
			this.position += 4;
			var high = this.view.getUint32(this.position, this.littleEndian);
			this.position += 4;
			return (high * Math.pow(2, 32)) + low;
		},

		readUint64Array: function(size) {
			var array = [];
			for (var i = 0; i < size; i++) {
				array.push(this.readUint64());
			}
			return array;
		},

		readUint32Array: function(size) {
			var array = [];
			for (var i = 0; i < size; i++) {
				array.push(this.readUint32());
			}
			return array;
		},

		readAsciiString: function(size) {
			var offset = this.offset + this.position;
			this.position += size;
			return String.fromCharCode.apply(null, new Uint8Array(this.buffer, offset, size)).replace(/\0/g, '');
		},


		readInt8: function() {
			return this.view.getInt8(this.position++, this.littleEndian);
		},

		readUint8: function() {
			return this.view.getUint8(this.position++, this.littleEndian);
		},

		readInt16: function() {
			var value = this.view.getInt16(this.position, this.littleEndian);
			this.position += 2;
			return value;
		},

		readUint16: function() {
			var value = this.view.getUint16(this.position, this.littleEndian);
			this.position += 2;
			return value;
		},

		readInt32: function() {
			var value = this.view.getInt32(this.position, this.littleEndian);
			this.position += 4;
			return value;
		},

		readUint32: function() {
			var value = this.view.getUint32(this.position, this.littleEndian);
			this.position += 4;
			return value;
		},

		readFloat32: function() {
			var value = this.view.getFloat32(this.position, this.littleEndian);
			this.position += 4;
			return value;
		},

		readFloat64: function() {
			var value = this.view.getFloat64(this.position, this.littleEndian);
			this.position += 8;
			return value;
		}
	};
	
}(self.JACERE = self.JACERE || {}));