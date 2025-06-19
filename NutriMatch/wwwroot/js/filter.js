
document.getElementById("submitFilter").addEventListener("click", async function () {
const form = document.getElementById("filterForm");
const data = new FormData(form);

const response = await fetch("/Recipes/Filter", {
    method: "POST",
    body: data,
    headers: {
        "X-Requested-With": "XMLHttpRequest"
    }
});

if (response.ok) {
    const html = await response.text();
    document.getElementById("resultsContainer").innerHTML = html;
    console.log(html);
} else {
    alert("Something went wrong while fetching recipes.");
}
});
