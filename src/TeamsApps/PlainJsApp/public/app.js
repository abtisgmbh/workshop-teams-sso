(function(w, appRootElement) {
  var p = document.createElement('p');
  p.textContent = "Hallo Peter";
  appRootElement.appendChild(p);
  console.log('Hallo peter');
})(window, document.getElementById("root"));