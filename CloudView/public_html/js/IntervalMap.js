
(function(JACERE) {
	
	JACERE.IntervalMapIndex = function(index, ratio) {
		this.index = index;
		this.stretchRatio = ratio;
	};

	JACERE.IntervalMap = function(stretch, desiredDestinationBins, scaleDesiredBinsToStretchRange) {
		this.scaleDesiredBinsToStretchRange = scaleDesiredBinsToStretchRange;

		this.stretch = stretch;
		this.binCountDesired = desiredDestinationBins;
		this.binCountEstimated = this.binCountDesired;

		// determine how many total bins are required if the 
		// desired bin count is used for the stretch range
		if (this.scaleDesiredBinsToStretchRange)
			this.binCountEstimated = ~~(this.binCountEstimated / this.stretch.stretchRatio());

		this.binCount = this.binCountEstimated;
		this.binSize = (this.stretch.actualRange() / this.binCount);

		this.actualMaxIndex = ~~(this.stretch.actualRange() / this.binSize);

		this.stretchMinIndex = ~~((this.stretch.stretchMin - this.stretch.actualMin) / this.binSize);
		this.stretchMaxIndex = ~~((this.stretch.stretchMax - this.stretch.actualMin) / this.binSize);
	};

	JACERE.IntervalMap.prototype = {

		constructor: JACERE.IntervalMap,
		
		getInterval: function(actualValue) {
			return ((actualValue - this.stretch.actualMin) / this.binSize);
		},

		getIntervals: function() {
			var start = 0;
			var stretchStart = this.stretchMinIndex;
			var stretchEnd = this.stretchMaxIndex;
			var end = this.actualMaxIndex;

			var inverseStretchRange = 1.0 / this.stretchMaxIndex;

			var intervals = [];

			for (var i = start; i < stretchStart; i++)
				intervals.push(new JACERE.IntervalMapIndex(i, 0.0));

			for (var i = stretchStart; i <= stretchEnd; i++)
				intervals.push(new JACERE.IntervalMapIndex(i, (i - stretchStart) * inverseStretchRange));

			for (var i = stretchEnd + 1; i < end; i++)
				intervals.push(new JACERE.IntervalMapIndex(i, 1.0));

			return intervals;
		}
		
	};
	
}(self.JACERE = self.JACERE || {}));