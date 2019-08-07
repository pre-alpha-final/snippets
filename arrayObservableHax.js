Observable.fromArray = (array) => {
   return new Observable((observer)=>{
      array.forEach((value)=>{
         observer.next(value)
      });
      const original = array.push;
      array.push = (value)=>{
         observer.next(value);
         original.call(array, value)
      }
   })
};