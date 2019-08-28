private synchronousSleepHack(milisecondTimeout: number) {
  const start = new Date().getTime(), expire = start + milisecondTimeout;
  while (new Date().getTime() < expire) { }
  return;
}
