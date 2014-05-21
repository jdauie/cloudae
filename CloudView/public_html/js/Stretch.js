(function(JACERE) {
	
	JACERE.StretchBase = function(actualMin, actualMax, stretchMin, stretchMax) {
		this.actualMin = actualMin;
		this.actualMax = actualMax;
	
		this.stretchMin = stretchMin;
		this.stretchMax = stretchMax;
	};

	JACERE.StretchBase.prototype = {
		
		constructor: JACERE.StretchBase,
		
		stretchRatio: function() {
			return (this.stretchRange() / this.actualRange());
		},

		actualRange: function() {
			return (this.actualMax - this.actualMin + 1);
		},

		stretchRange: function() {
			return (this.stretchMax - this.stretchMin + 1);
		}
	};
	
	JACERE.StdDevStretch = function(actualMin, actualMax, stats, numDeviationsFromMean) {
		this.stats = stats;
		this.deviations = numDeviationsFromMean;

		var totalDeviationFromMean = (this.deviations * stats.stdDev);
		var stretchMin = Math.max(actualMin, stats.mean - totalDeviationFromMean);
		var stretchMax = Math.min(actualMax, stats.mean + totalDeviationFromMean);
		
		JACERE.StretchBase.call(this, actualMin, actualMax, stretchMin, stretchMax);
	};
	
	JACERE.StdDevStretch.prototype = Object.create(JACERE.StretchBase.prototype);
	
	JACERE.MinMaxStretch = function(actualMin, actualMax) {
		JACERE.StretchBase.call(this, actualMin, actualMax, actualMin, actualMax);
	};
	
	JACERE.MinMaxStretch.prototype = Object.create(JACERE.StretchBase.prototype);
	
}(self.JACERE = self.JACERE || {}));