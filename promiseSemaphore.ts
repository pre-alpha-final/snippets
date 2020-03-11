stuffHappened: () => void;
stuffHappeningPromise = new Promise<void>(function(resolve: void) {
  this.stuffHappened = resolve;
}.bind(this));