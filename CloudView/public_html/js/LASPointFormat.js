
(function(JACERE) {
	
	function getBytesFromType(type) {
		switch (type) {
			case 'Int8':
			case 'Uint8':
				return 1;
			case 'Int16':
			case 'Uint16':
				return 2;
			case 'Int32':
			case 'Uint32':
			case 'Float32':
				return 4;
			case 'Uint64':
			case 'Float64':
				return 8;
		}
	}
	
	function ByteField(name, type) {
		this.name = name;
		this.type = type;
		this.bytes = getBytesFromType(type);
	}
	
	ByteField.prototype.toString = function() {
		return String.format('{0} ({1} bytes)', this.name, this.bytes);
	};
	
	function BitField(bits, name) {
		this.name = name;
		this.bits = bits;
	}
	
	BitField.prototype.toString = function() {
		return String.format('{0} ({1} bits)', this.name, this.bits);
	};
	
	function FieldGroup(name, fields, tuple) {
		this.name = name;
		this.fields = [];
		this.tuple = (tuple === true);
		
		this.append = function(field) {
			if (typeof field === 'string') {
				field = getCommonPointFormatField(field);
			}
			this.fields.push(field);
			return this;
		};
		
		for (var i = 0; i < fields.length; i++) {
			this.append(fields[i]);
		}
	}
	
	/*FieldGroup.prototype.toString = function() {
		return this.name ? this.name : '[group]';
	};*/
	
	function getCommonPointFormatField(name) {
		switch(name) {
			case 'XYZ':
				return new FieldGroup('XYZ', [
					new ByteField('X', 'Int32'),
					new ByteField('Y', 'Int32'),
					new ByteField('Z', 'Int32')
				], true);
			
			case 'Intensity':
				return new ByteField('Intensity', 'Uint16');
			
			case 'UserData':
				return new ByteField('UserData', 'Uint8');
			
			case 'PointSourceID':
				return new ByteField('PointSourceID', 'Uint16');
			
			case 'GPSTime':
				return new ByteField('GPSTime', 'Float64');
			
			case 'RGB':
				return new FieldGroup('RGB', [
					new ByteField('R', 'Uint16'),
					new ByteField('G', 'Uint16'),
					new ByteField('B', 'Uint16')
				], true);
			
			case 'NIR':
				return new ByteField('NIR', 'Uint16');
			
			case 'WavePackets':
				return new FieldGroup('WavePackets', [
					new ByteField('WavePacketDescriptorIndex', 'Uint8'),
					new ByteField('ByteOffsetToWaveFormData', 'Uint64'),
					new ByteField('WaveformPacketSizeInBytes', 'Uint32'),
					new ByteField('ReturnPointWaveformLocation', 'Float32'),
					new FieldGroup('ParametricLineEquation', [
						new ByteField('Xt', 'Float32'),
						new ByteField('Yt', 'Float32'),
						new ByteField('Zt', 'Float32')
					], true)
				]);
		}
	};
	
	function getPointFormatFields(format) {
		switch (format) {
			case 0: return new FieldGroup(null, [
				'XYZ',
				'Intensity',

				new FieldGroup('ReturnInfo', [
					new BitField(3, 'ReturnNumber'),
					new BitField(3, 'NumberOfReturns'),
					new BitField(1, 'ScanDirection'),
					new BitField(1, 'EdgeOfFlightline')
				]),

				new FieldGroup('Classification', [
					new BitField(5, 'Classification'),
					new BitField(1, 'Synthetic'),
					new BitField(1, 'KeyPoint'),
					new BitField(1, 'WithHeld')
				]),

				new ByteField('ScanAngleRank', 'Int8'),
				'UserData',
				'PointSourceID'
			]);
			
			case 1: return getPointFormatFields(0).append('GPSTime');
			case 2: return getPointFormatFields(0).append('RGB');
			case 3: return getPointFormatFields(2).append('GPSTime');
			case 4: return getPointFormatFields(1).append('WavePackets');
			case 5: return getPointFormatFields(3).append('WavePackets');
			
			case 6: return new FieldGroup(null, [
				'XYZ',
				'Intensity',

				new FieldGroup('ReturnInfo', [
					new BitField(4, 'ReturnNumber'),
					new BitField(4, 'NumberOfReturns')
				]),

				new FieldGroup('ClassificationFlags', [
					new BitField(1, 'Synthetic'),
					new BitField(1, 'KeyPoint'),
					new BitField(1, 'WithHeld'),
					new BitField(1, 'Overlap')
				]),

				new BitField(2, 'ScannerChannel'),
				new BitField(1, 'ScanDirectionFlag'),
				new BitField(1, 'EdgeOfFlightLine'),

				new ByteField('Classification', 'Uint8'),
				'UserData',
				new ByteField('ScanAngle', 'Int16'),
				'PointSourceID',
				'GPSTime'
			]);
			
			case 7: return getPointFormatFields(6).append('RGB');
			case 8: return getPointFormatFields(7).append('NIR');
			case 9: return getPointFormatFields(6).append('WavePackets');
			case 10: return getPointFormatFields(7).append('WavePackets');
		}
	}
	
	function getLeafFields(node) {
		return node.fields ? node.fields : [node];
	}
	
	function getPointFormatFieldsFlat(format) {
		var tree = getPointFormatFields(format);
		var fields = [];
		
		for (var i = 0; i < tree.fields.length; i++) {
			var leaves = getLeafFields(tree.fields[i]);
			[].push.apply(fields, leaves);
		}
		return fields;
	}
	
	function getPointFormat(reader, map) {
		var format = reader.info.header.pointDataRecordFormat;
		var fields = getPointFormatFieldsFlat(format);
		return createPointFormatFromFlatFields2(reader, fields, map);
	}
	
	JACERE.getPointFormatFields = getPointFormatFieldsFlat;
	JACERE.getPointFormat = getPointFormat;
	
	function createPointFormatFromFlatFields(header, fields, map) {
		var code = [];
		
		//code.push(String.format('function PointFormat{0}(view, offset) {', format));
		
		var offset = 0;
		for (var i = 0; i < fields.length; i++) {
			var field = fields[i];
			if (field.bits) {
				// group into byte
				var bitFields = [field];
				var bits = field.bits;
				var readByte = false;
				while (bits < 8) {
					var bitField = fields[++i];
					bitFields.push(bitField);
					bits += bitField.bits;
					readByte = readByte || map[bitField.name];
				}
				
				if (readByte) {
					code.push(String.format('var byte{0} = view.getUint8(offset + {0}, true);', offset));
				}
				
				var bitOffset = 0;
				for (var j = 0; j < bitFields.length; j++) {
					var bitField = bitFields[j];
					var name = bitField.name;
					
					if (map[name]) {
						name = map[name];
						
						code.push(String.format('this.{0} = (byte{3} & {1}) >> {2};', bitField.name, (((1 << bitField.bits) - 1) << bitOffset), bitOffset, offset));
					}
					
					bitOffset += bitField.bits;
				}
				
				offset += 1;
			}
			else {
				var transform = '';
				var name = field.name;
				if (name === 'X' || name === 'Y' || name === 'Z') {
					var key = field.name.toLowerCase();
					transform = String.format(' * {0} + {1}', header.quantization.scale[key], header.quantization.offset[key]);
				}
				
				if (map[name]) {
					name = map[name];
					
					code.push(String.format('this.{0} = view.get{1}(offset + {2}, true){3};', name, field.type, offset, transform));
				}
				
				offset += field.bytes;
			}
		}
		
		//code.push('}');
		
		//return code.join('\n');
		
		return new Function('view', 'offset', code.join('\n'));
	}
	
	function createPointFormatFromFlatFields2(reader, fields, map) {
		var code_pre = [];
		var code = [];
		
		code_pre.push('var view = this.reader.view;');
		code_pre.push(String.format('var offset = ({0} + pointOffset) * {1};', reader.pointIndex, reader.info.header.pointDataRecordLength));
		
		var offset = 0;
		for (var i = 0; i < fields.length; i++) {
			var field = fields[i];
			if (field.bits) {
				// group into byte
				var bitFields = [field];
				var bits = field.bits;
				var readByte = false;
				while (bits < 8) {
					var bitField = fields[++i];
					bitFields.push(bitField);
					bits += bitField.bits;
					readByte = readByte || map[bitField.name];
				}
				
				if (readByte) {
					code_pre.push(String.format('var byte{0} = view.getUint8(offset + {0}, true);', offset));
				}
				
				var bitOffset = 0;
				for (var j = 0; j < bitFields.length; j++) {
					var bitField = bitFields[j];
					var name = bitField.name;
					
					if (map[name]) {
						name = map[name];
						
						code.push(String.format('{0}: (byte{3} & {1}) >> {2}', bitField.name, (((1 << bitField.bits) - 1) << bitOffset), bitOffset, offset));
					}
					
					bitOffset += bitField.bits;
				}
				
				offset += 1;
			}
			else {
				var transform = '';
				var name = field.name;
				if (name === 'X' || name === 'Y' || name === 'Z') {
					var key = field.name.toLowerCase();
					transform = String.format(' * {0} + {1}', reader.info.header.quantization.scale[key], reader.info.header.quantization.offset[key]);
				}
				
				if (map[name]) {
					name = map[name];
					
					code.push(String.format('{0}: view.get{1}(offset + {2}, true){3}', name, field.type, offset, transform));
				}
				
				offset += field.bytes;
			}
		}
		
		code = String.format([
			code_pre.join('\n'),
			'return {',
			code.join(',\n'),
			'};'
		].join('\n'));
		
		return new Function('pointOffset', code);
	}
	
	/*function PointFormat1(view, offset) {
		var x = view.getInt32(offset + 0, true);
		var y = view.getInt32(offset + 4, true);
		var z = view.getInt32(offset + 8, true);
		
		var intensity = view.getUint16(offset + 12, true);
		
		var byte1 = view.getUint8(offset + 14, true);
		var returnNumber = (byte1 & (((1 << 3) - 1) << 0)) >> 0;
		var numReturns   = (byte1 & (((1 << 3) - 1) << 3)) >> 3;
		var scanDir      = (byte1 & (((1 << 1) - 1) << 6)) >> 6;
		var edge         = (byte1 & (((1 << 1) - 1) << 7)) >> 7;
		
		var byte2 = view.getUint8(offset + 15, true);
		//...
		
		var scanAngleRank = view.getInt8(offset + 16, true);
		var userData      = view.getUint8(offset + 17, true);
		var pointSourceID = view.getUint16(offset + 18, true);
		var gpsTime       = view.getFloat64(offset + 20, true);
	}*/
	
}(self.JACERE = self.JACERE || {}));