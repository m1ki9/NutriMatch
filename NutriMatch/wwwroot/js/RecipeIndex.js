document.addEventListener('DOMContentLoaded', function() {
    const searchInput = document.getElementById('searchInput');
    
    
    searchInput.addEventListener('input', function() {
        filterRecipes();
    });
    
    
    searchInput.addEventListener('keypress', function(e) {
        if (e.key === 'Enter') {
           filterRecipes();
        }
    });

    filterRecipes();
});

function showRecipeDetails(recipeId) {

    const clickedCard = event.currentTarget;
    clickedCard.classList.add('loading');
    
    fetch(`/Recipes/Details/${recipeId}`)
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.text();
        })
        .then(html => {
            const modalContainer = document.getElementById('modalWindow');
            modalContainer.innerHTML = html;
            
            const modalElement = modalContainer.querySelector('.modal');
            if (modalElement) {
                const modal = new bootstrap.Modal(modalElement);
                modal.show();
                
                modalElement.addEventListener('hidden.bs.modal', function () {
                    modalContainer.innerHTML = '';
                    clickedCard.classList.remove('loading');
                }); 
                
                modalElement.addEventListener('shown.bs.modal', function () {
                    clickedCard.classList.remove('loading');
                });

            } else {    
                clickedCard.classList.remove('loading');
            }
        })
        .catch(err => {
            console.error("Failed to fetch recipe details", err);
            alert("Failed to load recipe details. Please try again.");
            clickedCard.classList.remove('loading');
        });
    }

function toggleFavorite(button) {
    const icon = button.querySelector('i');
    if (icon.classList.contains('far')) {
        icon.classList.remove('far');
        icon.classList.add('fas');
        button.classList.add('active');
    } else {
        icon.classList.remove('fas');
        icon.classList.add('far');
        button.classList.remove('active');
    }
}



function updateSlider(type) {
    const minSlider = document.getElementById(type + 'Min');
    const maxSlider = document.getElementById(type + 'Max');
    const valueDisplay = document.getElementById(type + 'Value');
    const fill = document.getElementById(type + 'Fill');
    
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
    
    valueDisplay.textContent = minVal + ' - ' + maxVal;
    
    const min = parseInt(minSlider.min);
    const max = parseInt(minSlider.max);
    const range = max - min;
    
    const leftPercent = ((minVal - min) / range) * 100;
    const rightPercent = ((maxVal - min) / range) * 100;
    
    fill.style.left = leftPercent + '%';
    fill.style.width = (rightPercent - leftPercent) + '%';
    
    filterRecipes();
}

function filterRecipes() {
    const calories = {
        min: parseInt(document.getElementById('caloriesMin').value),
        max: parseInt(document.getElementById('caloriesMax').value)
    };
    const protein = {
        min: parseInt(document.getElementById('proteinMin').value),
        max: parseInt(document.getElementById('proteinMax').value)
    };
    const carbs = {
        min: parseInt(document.getElementById('carbsMin').value),
        max: parseInt(document.getElementById('carbsMax').value)
    };
    const fats = {
        min: parseInt(document.getElementById('fatsMin').value),
        max: parseInt(document.getElementById('fatsMax').value)
    };
    
    const searchTerm = document.getElementById('searchInput').value.toLowerCase();
    
    const recipeCards = document.querySelectorAll('.recipe-card');
    let visibleCount = 0;
    
    recipeCards.forEach(card => {
        const title = card.querySelector('.recipe-title').textContent.toLowerCase();
        
        const recipeCalories = parseInt(card.dataset.calories) || 0;
        const recipeProtein = parseInt(card.dataset.protein) || 0;
        const recipeCarbs = parseInt(card.dataset.carbs) || 0;
        const recipeFats = parseInt(card.dataset.fat) || 0;
        
        const matchesSearch = searchTerm === '' || title.includes(searchTerm);
        
        const matchesMacros = 
            recipeCalories >= calories.min && recipeCalories <= calories.max &&
            recipeProtein >= protein.min && recipeProtein <= protein.max &&
            recipeCarbs >= carbs.min && recipeCarbs <= carbs.max &&
            recipeFats >= fats.min && recipeFats <= fats.max;
        
        if (matchesSearch && matchesMacros) {
            card.style.display = 'block';
            visibleCount++;
        } else {
            card.style.display = 'none';
        }
    });
    
    const resultsCount = document.querySelector('.results-count');
    resultsCount.innerHTML = `<i class="fas fa-utensils me-2"></i>Showing <strong>${visibleCount} recipes</strong> matching your criteria`;
}



function resetFilters() {
    document.getElementById('caloriesMin').value = 0;
    document.getElementById('caloriesMax').value = 2000;
    document.getElementById('proteinMin').value = 0;
    document.getElementById('proteinMax').value = 150;
    document.getElementById('carbsMin').value = 0;
    document.getElementById('carbsMax').value = 150;
    document.getElementById('fatsMin').value = 0;
    document.getElementById('fatsMax').value = 150;
    document.getElementById('searchInput').value = '';
    
    updateSlider('calories');
    updateSlider('protein');
    updateSlider('carbs');
    updateSlider('fats');

    filterRecipes();
}

function openDeleteModal(recipeId) {
    const deleteButton = event.target.closest('button');
    deleteButton.classList.add('loading');

    const recipeModalContainer = document.getElementById('modalWindow');
    const recipeModalElement = recipeModalContainer.querySelector('.modal');
    const savedRecipeHtml = recipeModalContainer.innerHTML;

    let recipeModalWasOpen = false;
    if (recipeModalElement && recipeModalElement.classList.contains('show')) {
        const recipeModalInstance = bootstrap.Modal.getInstance(recipeModalElement);
        if (recipeModalInstance) {
            recipeModalInstance.hide();
            recipeModalWasOpen = true;
        }
    }

    fetch(`/Recipes/Delete/${recipeId}`)
        .then(response => response.text())
        .then(html => {
            const deleteModalContainer = document.getElementById('modalWindowDelete');
            deleteModalContainer.innerHTML = html;

            const deleteModalElement = deleteModalContainer.querySelector('.modal');
            if (deleteModalElement) {
                const deleteModal = new bootstrap.Modal(deleteModalElement);
                deleteModal.show();

                deleteModalElement.addEventListener('hidden.bs.modal', function () {
                    deleteButton.classList.remove('loading');
                    deleteModalContainer.innerHTML = '';

                    if (recipeModalWasOpen && savedRecipeHtml.trim() !== '') {
                        recipeModalContainer.innerHTML = savedRecipeHtml;
                        const restoredModal = recipeModalContainer.querySelector('.modal');
                        if (restoredModal) {
                            const restoredInstance = new bootstrap.Modal(restoredModal);
                            restoredInstance.show();
                        }
                    }
                });

                deleteModalElement.addEventListener('shown.bs.modal', function () {
                    deleteButton.classList.remove('loading');
                });

            } else {
                deleteButton.classList.remove('loading');
            }
        })
        .catch(error => {
            console.error('Error loading delete modal:', error);
            deleteButton.classList.remove('loading');
            location.href = `/Recipes/Delete/${recipeId}`;
        });
}


