
(function(JACERE) {
	
	JACERE.PointReader = function(info, buffer, pointIndex, pointCount) {
		this.reader = new JACERE.BinaryReader(buffer);
		this.info = info;
		this.points = pointCount;
		this.pointIndex = pointIndex;
		this.currentPointIndex = pointIndex;
		this.endIndex = pointIndex + pointCount;
		
		// z
		// intensity
		// return num
		// num returns
		// return num / num returns ? (maybe a generic way of operating on fields)?
		// classification
		// scan angle rank
		// user data
		// point source id
		// gps time
		// rgb
		
		/*this.fields = JACERE.getPointFormatFields(this.info.header.pointDataRecordFormat);
		console.log('----------');
		for (var i = 0; i < this.fields.length; i++) {
			console.log(String(this.fields[i]));
		}*/
		
		this.offsetToRGB = 0;
		switch (this.info.header.pointDataRecordFormat) {
			case 2:
			case 3:
			case 5:
				this.offsetToRGB = 20;//8;
				break;
			case 7:
			case 8:
			case 10:
				this.offsetToRGB = 30;//18;
				break;
		}
		this.hasRGB = (this.offsetToRGB > 0);
		
		
		this.readPointInternal = function(view, position) {
			var q = this.info.header.quantization;
			/*var point = {
				x: info.header.extent.min.x,
				y: info.header.extent.min.y,
				z: info.header.extent.min.z
			};*/
			var point = {
				x: view.getInt32(position + 0, true) * q.scale.x + q.offset.x,
				y: view.getInt32(position + 4, true) * q.scale.y + q.offset.y,
				z: view.getInt32(position + 8, true) * q.scale.z + q.offset.z
			};
			
			/*var color = this.info.colorMap.getColor(point.z);
			point.color = 
				(~~(255 * color.r) << 0) | 
				(~~(255 * color.g) << 8) | 
				(~~(255 * color.b) << 16);*/
			
			if (this.hasRGB) {
				point.color = 
					(~~((255/(256*256)) * view.getUint16(position + this.offsetToRGB + 0, true)) << 0) | 
					(~~((255/(256*256)) * view.getUint16(position + this.offsetToRGB + 2, true)) << 8) | 
					(~~((255/(256*256)) * view.getUint16(position + this.offsetToRGB + 4, true)) << 16);
			}

			return point;
		};
	};
	
	JACERE.PointReader.prototype = {
		
		constructor: JACERE.PointReader,
		
		reset: function() {
			this.currentPointIndex = this.pointIndex;
		},
		
		readPointColor: function() {
			return this.readPoint().color;
		},
		
		readPoint: function() {
			/*if (this.currentPointIndex >= this.endIndex) {
				return null;
			}*/

			return this.readPointInternal(this.reader.view, this.info.header.pointDataRecordLength * this.currentPointIndex++);
		},
		
		readPoint4: function() {
			if (this.currentPointIndex >= this.points) {
				return null;
			}

			this.reader.seek(this.currentPointIndex * this.info.header.pointDataRecordLength);
			//var point = this.reader.readUnquantizedPoint3D(this.info.header.quantization);
			
			var q = this.info.header.quantization;
			var point = {
				x: this.reader.readInt32() * q.scale.x + q.offset.x,
				y: this.reader.readInt32() * q.scale.y + q.offset.y,
				z: this.reader.readInt32() * q.scale.z + q.offset.z
			};

			if (this.hasRGB) {
				this.reader.skip(this.offsetToRGB);
				// rgb should be scaled to (256^2) according to the spec
				var scale = (256*256);
				point.r = this.reader.readUint16() / scale;
				point.g = this.reader.readUint16() / scale;
				point.b = this.reader.readUint16() / scale;
			}

			this.currentPointIndex++;

			return point;
		}
	};
	
}(self.JACERE = self.JACERE || {}));