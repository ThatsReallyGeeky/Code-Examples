function sumArr(arr){

	let val = 0;

	for(let i = 0; i < arr.length; i++){
  
		val += arr[i];
  
	}
  
	return val;

}

function reverseArr(arr){

	let newArr = arr;

	return newArr.slice().reverse();

}

function reverseArrManual(arr){

	let newArr = [];
  
	for(let i = 0; i < arr.length; i++){

		newArr.unshift(arr[i]);

	}
  
	return newArr;

}

let numbers = [1,2,3,4];

console.log(sumArr(numbers));
console.log("Numbers after sum: " + numbers);
console.log("Numbers after reverse: " + reverseArr(numbers));
console.log("Numbers after reverse manual: " + reverseArrManual(numbers));

