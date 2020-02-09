function stringifyWithFunctions(object) {
  return JSON.stringify(object, (key, val) => {
    if (typeof val === 'function') {
      return `(${val})`;
    }
    return val;
  });
};

function parseWithFunctions(obj) {
  return JSON.parse(obj, (k, v) => {
    if (typeof v === 'string' && (v.indexOf('function(') >= 0 || v.indexOf(') => ') >= 0)) {
      return eval(v);
    }
    return v;
  });
};