
(function(JACERE) {
	
	JACERE.LASInfo = function(file) {
		this.file = file;
		this.startTime = Date.now();
		//this.endTime = null;
		this.texture = null;
		this.tileIndex = 0;

		this.settings = {
			loader: JACERE.Util.clone(settings.loader),
			render: JACERE.Util.clone(settings.render)
		};

		this.geometry = {
			bounds: null,
			progress: null,
			chunks: []
		};
	};
	
	JACERE.LASInfo.prototype = {
		
		constructor: JACERE.LASInfo,
		
		setHeader: function(header, tiles, stats) {
			this.header = header;
			this.tiles = tiles;
			this.stats = stats;
			this.step = Math.ceil(header.numberOfPointRecords / Math.min(this.settings.loader.maxPoints, header.numberOfPointRecords));
			this.radius = header.extent.size().length() / 2;

			this.update();
		},

		getPointReader: function(buffer, points) {
			return new JACERE.PointReader(this, buffer, points);
		},

		updateRenderSettings: function() {
			this.settings.render = JACERE.Util.clone(settings.render);
			this.update();
		},

		update: function() {
			this.updateColorMap();
			this.updateTexture();
			this.updateMaterial();

			// update attributes if necessary
			this.forceColorUpdate();
		},

		forceColorUpdate: function() {
			if (!this.material || this.geometry.chunks.length === 0 || !this.geometry.chunks[0].geometry.attributes.color)
				return;

			for (var i = 0; i < this.geometry.chunks.length; i++) {
				var obj = this.geometry.chunks[i];
				updateChunkOld(obj);
			}
		},

		updateMaterial: function() {
			if (this.material) {
				if (!this.material.vertexColors) {
					this.material.uniforms.size.value = this.settings.render.pointSize;
				}
				else {
					this.material.size = this.settings.render.pointSize;
				}
				this.material.needsUpdate = true;
			}
		},

		updateColorMap: function() {
			var ramp = JACERE.ColorRamp.presets[this.settings.render.colorRamp];
			if (this.settings.render.invertRamp) {
				ramp = ramp.reverse();
			}

			var min = this.header.extent.min;
			var max = this.header.extent.max;

			var stretch = (this.settings.render.useStats && this.stats)
				? new JACERE.StdDevStretch(min.z, max.z, this.stats, 2)
				: new JACERE.MinMaxStretch(min.z, max.z);

			this.colorMap = new JACERE.CachedColorMap(ramp, stretch, this.settings.render.colorValues);
		},

		updateTexture: function() {
			if (!this.texture) {
				this.texture = new THREE.Texture(this.createGradient(this.colorMap));
			}
			else {
				this.createGradient(this.colorMap, this.texture.image);
			}
			this.texture.needsUpdate = true;
		},

		createGradient: function(map, canvas) {
			var size = map.length;

			if (!canvas) {
				canvas = document.createElement('canvas');
				canvas.width = size;
				canvas.height = 1;
			}

			var context = canvas.getContext('2d');

			context.rect(0, 0, size, 1);
			var gradient = context.createLinearGradient(0, 0, size, 0);

			//var stops = map.ramp.colors.length;
			var stops = map.length;
			for (var i = 0; i < stops; ++i) {
				var ratio = i / (stops - 1);
				//gradient.addColorStop(ratio, '#' + map.ramp.getColor(ratio).getHexString());
				gradient.addColorStop(ratio, '#' + map.bins[i].getHexString());
			}

			context.fillStyle = gradient;
			context.fill();

			return canvas;
		}
	};

	JACERE.PointReader = function(info, buffer, points) {
		this.reader = new JACERE.BinaryReader(buffer);
		this.info = info;
		this.points = points;
		this.currentPointIndex = 0;
	};
	
	JACERE.PointReader.prototype = {
		
		constructor: JACERE.PointReader,
		
		readPoint: function() {
			if (this.currentPointIndex >= this.points) {
				return null;
			}

			this.reader.seek(this.currentPointIndex * this.info.header.pointDataRecordLength);
			var point = this.reader.readUnquantizedPoint3D(this.info.header.quantization);

			if (this.info.header.pointDataRecordFormat === 2) {
				this.reader.skip(8);
				// this should be (256*256) according to the spec
				var scale = (256*256);
				point.r = this.reader.readUint16() / scale;
				point.g = this.reader.readUint16() / scale;
				point.b = this.reader.readUint16() / scale;
			}

			// debug
			//if (point.x === 0 && point.y === 0 && point.z === 0)
			//	throw "zero point";

			this.currentPointIndex++;

			return point;
		}
	};
	
}(self.JACERE = self.JACERE || {}));