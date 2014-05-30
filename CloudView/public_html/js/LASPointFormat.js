
(function(JACERE) {
	
	function ByteField(bytes, name) {
		this.name = name;
		this.bytes = bytes;
	}
	
	function BitField(bits, name) {
		this.name = name;
		this.bits = bits;
	}
	
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
					new ByteField(4, 'X'),
					new ByteField(4, 'Y'),
					new ByteField(4, 'Z')
				], true);
			
			case 'Intensity':
				return new ByteField(2, 'Intensity');
			
			case 'UserData':
				return new ByteField(1, 'UserData');
			
			case 'PointSourceID':
				return new ByteField(2, 'PointSourceID');
			
			case 'GPSTime':
				return new ByteField(8, 'GPSTime');
			
			case 'RGB':
				return new FieldGroup('RGB', [
					new ByteField(2, 'R'),
					new ByteField(2, 'G'),
					new ByteField(2, 'B')
				], true);
			
			case 'WavePackets':
				return new FieldGroup('WavePackets', [
					new ByteField(1, 'WavePacketDescriptorIndex'),
					new ByteField(8, 'ByteOffsetToWaveFormData'),
					new ByteField(4, 'WaveformPacketSizeInBytes'),
					new ByteField(4, 'ReturnPointWaveformLocation'),
					new FieldGroup('ParametricLineEquation', [
						new ByteField(4, 'Xt'),
						new ByteField(4, 'Yt'),
						new ByteField(4, 'Zt')
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

				new ByteField(1, 'ScanAngleRank'),
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

				new ByteField(1, 'Classification'),
				'UserData',
				new ByteField(2, 'ScanAngle'),
				'PointSourceID',
				'GPSTime'
			]);
			
			case 7: return getPointFormatFields(6).append('RGB');
			case 8: return getPointFormatFields(7).append('NIR');
			case 9: return getPointFormatFields(6).append('WavePackets');
			case 10: return getPointFormatFields(7).append('WavePackets');
		}
	}
	
	JACERE.getPointFormatFields = getPointFormatFields;
	
}(self.JACERE = self.JACERE || {}));