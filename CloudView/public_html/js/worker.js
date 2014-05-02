self.addEventListener('message', function(e) {
  var data = e.data;
  loadURL(data.url);
}, false);

function loadURL(url) {
	var xhr = new XMLHttpRequest();
	xhr.open('GET', url, true);
	//xhr.setRequestHeader('Range', 'bytes=100-200');
	xhr.responseType = 'arraybuffer';
	xhr.onprogress = updateProgress;
	xhr.onload = function(e) {
		var arrayBuffer = this.response;
		self.postMessage(arrayBuffer, [arrayBuffer]);
	};

	xhr.send();
}

function updateProgress(evt) {
	if (evt.lengthComputable) {
		var percentComplete = (evt.loaded / evt.total) * 100;
		self.postMessage({progress: ~~percentComplete});
	}
}
