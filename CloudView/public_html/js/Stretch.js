
function StretchBase(/*actualMin, actualMax*/) {
	
	//this.actualMin = actualMin;
	//this.actualMax = actualMax;
	
	this.stretchRatio = function() {
		return (this.stretchRange() / this.actualRange());
	};

	this.actualRange = function() {
		return (this.actualMax - this.actualMin + 1);
	};

	this.stretchRange = function() {
		return (this.stretchMax - this.stretchMin + 1);
	};
}

StdDevStretch.prototype = new StretchBase();
StdDevStretch.prototype.constructor = StdDevStretch;

function StdDevStretch(actualMin, actualMax, stats, numDeviationsFromMean) {
	this.actualMin = actualMin;
	this.actualMax = actualMax;
	this.stats = stats;
	this.deviations = numDeviationsFromMean;
	
	var totalDeviationFromMean = (this.deviations * stats.stdDev);
	this.stretchMin = Math.max(this.actualMin, stats.mean - totalDeviationFromMean);
	this.stretchMax = Math.min(this.actualMax, stats.mean + totalDeviationFromMean);
}

MinMaxStretch.prototype = new StretchBase();
MinMaxStretch.prototype.constructor = MinMaxStretch;

function MinMaxStretch(actualMin, actualMax) {
	this.actualMin = actualMin;
	this.actualMax = actualMax;
	
	this.stretchMin = this.actualMin;
	this.stretchMax = this.actualMax;
}
