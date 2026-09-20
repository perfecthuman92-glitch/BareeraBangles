(() => {
  const container = document.getElementById('sizeRows');
  const addBtn = document.getElementById('addSizeRow');
  if (!container || !addBtn) return;

  let index = window.bareeraSizeIndex || container.querySelectorAll('.size-row').length;

  function addRow(size, category, measurement, stock) {
    const row = document.createElement('div');
    row.className = 'size-row';
    row.innerHTML = `
      <input type="hidden" name="Sizes[${index}].Id" value="0" />
      <input name="Sizes[${index}].Size" placeholder="Size label" value="${size || ''}" />
      <select name="Sizes[${index}].SizeCategory">
        <option value="0" ${category === 0 ? 'selected' : ''}>Adult</option>
        <option value="1" ${category === 1 ? 'selected' : ''}>BabyKids</option>
        <option value="2" ${category === 2 ? 'selected' : ''}>Unisex</option>
      </select>
      <input name="Sizes[${index}].Measurement" placeholder="Measurement (optional)" value="${measurement || ''}" />
      <input name="Sizes[${index}].StockQuantity" type="number" value="${stock ?? 10}" />
      <label><input name="Sizes[${index}].IsActive" type="checkbox" value="true" checked /> Active</label>
      <label><input name="Sizes[${index}].Remove" type="checkbox" value="true" /> Remove</label>
    `;
    container.appendChild(row);
    index += 1;
  }

  addBtn.addEventListener('click', () => addRow('', 0, '', 10));

  document.getElementById('addAdultSizes')?.addEventListener('click', () => {
    ['2.2', '2.4', '2.6', '2.8'].forEach(s => addRow(s, 0, `Inner diameter ${s}"`, 10));
  });

  document.getElementById('addKidsSizes')?.addEventListener('click', () => {
    const kids = [
      ['Newborn', 'approx. 1.4–1.6'],
      ['0–6 Months', 'approx. 1.6–1.8'],
      ['6–12 Months', 'approx. 1.8–2.0'],
      ['1–2 Years', 'approx. 2.0–2.2'],
      ['2–4 Years', 'approx. 2.2–2.4'],
      ['4–6 Years', 'approx. 2.4–2.6'],
      ['6–8 Years', 'approx. 2.6–2.8'],
      ['8–10 Years', 'approx. 2.8–3.0']
    ];
    kids.forEach(([label, m]) => addRow(label, 1, m, 8));
  });
})();
