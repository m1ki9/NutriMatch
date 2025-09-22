let currentFilters = {
  calories: { min: 0, max: 2000 },
  protein: { min: 0, max: 150 },
  carbs: { min: 0, max: 150 },
  fats: { min: 0, max: 150 },
};

document.addEventListener("DOMContentLoaded", function () {
  initializeFilters();
  updateFilterValues();
});

function itemMatchesFilters(item) {
  return (
    item.calories >= currentFilters.calories.min &&
    item.calories <= currentFilters.calories.max &&
    item.protein >= currentFilters.protein.min &&
    item.protein <= currentFilters.protein.max &&
    item.carbs >= currentFilters.carbs.min &&
    item.carbs <= currentFilters.carbs.max &&
    item.fats >= currentFilters.fats.min &&
    item.fats <= currentFilters.fats.max
  );
}

function areFiltersDefault() {
  return (
    currentFilters.calories.min === 0 &&
    currentFilters.calories.max === 2000 &&
    currentFilters.protein.min === 0 &&
    currentFilters.protein.max === 150 &&
    currentFilters.carbs.min === 0 &&
    currentFilters.carbs.max === 150 &&
    currentFilters.fats.min === 0 &&
    currentFilters.fats.max === 150
  );
}

function updateFilterValues() {
  document.getElementById(
    "caloriesValue"
  ).textContent = `${currentFilters.calories.min} - ${currentFilters.calories.max}`;
  document.getElementById(
    "proteinValue"
  ).textContent = `${currentFilters.protein.min} - ${currentFilters.protein.max}`;
  document.getElementById(
    "carbsValue"
  ).textContent = `${currentFilters.carbs.min} - ${currentFilters.carbs.max}`;
  document.getElementById(
    "fatsValue"
  ).textContent = `${currentFilters.fats.min} - ${currentFilters.fats.max}`;
}

function applyFilters() {
  currentFilters.calories.min = parseInt(
    document.getElementById("caloriesMin").value
  );
  currentFilters.calories.max = parseInt(
    document.getElementById("caloriesMax").value
  );
  currentFilters.protein.min = parseInt(
    document.getElementById("proteinMin").value
  );
  currentFilters.protein.max = parseInt(
    document.getElementById("proteinMax").value
  );
  currentFilters.carbs.min = parseInt(
    document.getElementById("carbsMin").value
  );
  currentFilters.carbs.max = parseInt(
    document.getElementById("carbsMax").value
  );
  currentFilters.fats.min = parseInt(document.getElementById("fatsMin").value);
  currentFilters.fats.max = parseInt(document.getElementById("fatsMax").value);

  updateFilterValues();
}

function openMenu(restaurantId) {
  const f = {
    minCalories: currentFilters.calories.min,
    maxCalories: currentFilters.calories.max,
    minProtein: currentFilters.protein.min,
    maxProtein: currentFilters.protein.max,
    minCarbs: currentFilters.carbs.min,
    maxCarbs: currentFilters.carbs.max,
    minFats: currentFilters.fats.min,
    maxFats: currentFilters.fats.max,
  };

  const query = new URLSearchParams(f).toString();

  const menuContainer = document.getElementById("menuItems");
  menuContainer.innerHTML =
    '<div class="text-center p-4"><i class="fas fa-spinner fa-spin"></i> Loading menu...</div>';

  const filterInfo = document.getElementById("filterInfo");
  if (!areFiltersDefault()) {
    filterInfo.style.display = "block";
  } else {
    filterInfo.style.display = "none";
  }

  const modal = new bootstrap.Modal(document.getElementById("menuModal"));
  modal.show();

  fetch(`/Restaurants/GetRestaurantMeals/${restaurantId}?${query}`)
    .then((response) => {
      console.log("Response status:", response.status);
      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }
      return response.text();
    })
    .then((html) => {
      console.log("Received HTML length:", html.length);
      menuContainer.innerHTML = html;
    })
    .catch((err) => {
      console.error("Failed to fetch menu details:", err);
      menuContainer.innerHTML = `
            <div class="alert alert-danger" role="alert">
                <i class="fas fa-exclamation-triangle me-2"></i>
                Failed to load menu details. Please try again.
                <br><small>Error: ${err.message}</small>
            </div>
        `;
    });
}

function toggleItemDetails(headerElement) {
  const menuItem = headerElement.closest(".menu-item");
  const details = menuItem.querySelector(".menu-item-details");
  const chevron = headerElement.querySelector(".chevron-icon");

  const isShown = details.classList.contains("show");

  details.classList.toggle("show", !isShown);
  chevron.classList.toggle("fa-chevron-up", !isShown);
  chevron.classList.toggle("fa-chevron-down", isShown);
}

function initializeFilters() {
  const sliders = [
    "caloriesMin",
    "caloriesMax",
    "proteinMin",
    "proteinMax",
    "carbsMin",
    "carbsMax",
    "fatsMin",
    "fatsMax",
  ];

  sliders.forEach((sliderId) => {
    document.getElementById(sliderId).addEventListener("input", function () {
      const type = sliderId.replace("Min", "").replace("Max", "");
      const isMin = sliderId.includes("Min");

      const minSlider = document.getElementById(type + "Min");
      const maxSlider = document.getElementById(type + "Max");

      if (isMin && parseInt(minSlider.value) > parseInt(maxSlider.value)) {
        maxSlider.value = minSlider.value;
      } else if (
        !isMin &&
        parseInt(maxSlider.value) < parseInt(minSlider.value)
      ) {
        minSlider.value = maxSlider.value;
      }

      currentFilters[type].min = parseInt(minSlider.value);
      currentFilters[type].max = parseInt(maxSlider.value);

      updateFilterValues();
    });
  });
}

function updateSlider(type) {
  const minSlider = document.getElementById(type + "Min");
  const maxSlider = document.getElementById(type + "Max");
  const valueDisplay = document.getElementById(type + "Value");
  const fill = document.getElementById(type + "Fill");

  let minVal = parseInt(minSlider.value);
  let maxVal = parseInt(maxSlider.value);

  if (minVal > maxVal) {
    if (event.target === minSlider) {
      maxSlider.value = minVal;
      maxVal = minVal;
    } else {
      minSlider.value = maxVal;
      minVal = maxVal;
    }
  }

  valueDisplay.textContent = minVal + " - " + maxVal;

  const min = parseInt(minSlider.min);
  const max = parseInt(minSlider.max);
  const range = max - min;

  const leftPercent = ((minVal - min) / range) * 100;
  const rightPercent = ((maxVal - min) / range) * 100;

  fill.style.left = leftPercent + "%";
  fill.style.width = rightPercent - leftPercent + "%";

  applyFilters();
}

function resetFilters() {
  document.getElementById("caloriesMin").value = 0;
  document.getElementById("caloriesMax").value = 2000;
  document.getElementById("proteinMin").value = 0;
  document.getElementById("proteinMax").value = 150;
  document.getElementById("carbsMin").value = 0;
  document.getElementById("carbsMax").value = 150;
  document.getElementById("fatsMin").value = 0;
  document.getElementById("fatsMax").value = 150;

  updateSlider("calories");
  updateSlider("protein");
  updateSlider("carbs");
  updateSlider("fats");
}
