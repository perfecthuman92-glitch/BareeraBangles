(() => {
  const header = document.getElementById('siteHeader');
  const navToggle = document.getElementById('navToggle');
  const siteNav = document.getElementById('siteNav');
  const toastHost = document.getElementById('toastHost');
  const cartCountEl = document.getElementById('cartCount');

  const onScroll = () => {
    if (!header) return;
    header.classList.toggle('scrolled', window.scrollY > 12);
  };
  onScroll();
  window.addEventListener('scroll', onScroll, { passive: true });

  navToggle?.addEventListener('click', () => {
    const open = siteNav?.classList.toggle('open');
    navToggle.setAttribute('aria-expanded', open ? 'true' : 'false');
  });

  function toast(message, isError) {
    if (!toastHost || !message) return;
    const el = document.createElement('div');
    el.className = 'toast';
    el.style.background = isError ? '#8a2a2a' : '#2a2327';
    el.textContent = message;
    toastHost.appendChild(el);
    setTimeout(() => el.remove(), 3200);
  }

  async function refreshCartCount() {
    try {
      const res = await fetch('/Cart/Count');
      if (!res.ok) return;
      const data = await res.json();
      if (cartCountEl) cartCountEl.textContent = data.count ?? 0;
    } catch { /* ignore */ }
  }
  async function refreshWishCount() {
    try {
      const res = await fetch('/Wishlist/Count');
      if (!res.ok) return;
      const data = await res.json();
      const el = document.getElementById('wishCount');
      if (el) el.textContent = data.count ?? 0;
    } catch { /* ignore */ }
  }
  refreshCartCount();
  refreshWishCount();
  refreshCompareCount();

  function antiforgeryToken() {
    return document.querySelector('#antiForgeryForm input[name="__RequestVerificationToken"]')?.value || '';
  }

  async function refreshCompareCount() {
    try {
      const res = await fetch('/Compare/Count');
      if (!res.ok) return;
      const data = await res.json();
      const el = document.getElementById('compareCount');
      if (el) el.textContent = data.count ?? 0;
    } catch { /* ignore */ }
  }

  document.querySelectorAll('.compare-btn').forEach(btn => {
    btn.addEventListener('click', async (e) => {
      e.preventDefault();
      e.stopPropagation();
      const productId = btn.getAttribute('data-product-id');
      const body = new URLSearchParams();
      body.set('productId', productId);
      body.set('__RequestVerificationToken', antiforgeryToken());
      try {
        const res = await fetch('/Compare/Toggle', {
          method: 'POST',
          headers: { 'X-Requested-With': 'XMLHttpRequest', 'Content-Type': 'application/x-www-form-urlencoded' },
          body
        });
        const data = await res.json();
        toast(data.message, !data.success);
        if (data.success) {
          const el = document.getElementById('compareCount');
          if (el) el.textContent = data.count ?? 0;
        }
      } catch {
        toast('Could not update compare', true);
      }
    });
  });

  // Cookie consent
  const cookieBanner = document.getElementById('cookieBanner');
  const cookieAccept = document.getElementById('cookieAccept');
  if (cookieBanner && !localStorage.getItem('bareeraCookieOk')) {
    cookieBanner.hidden = false;
  }
  cookieAccept?.addEventListener('click', () => {
    localStorage.setItem('bareeraCookieOk', '1');
    cookieBanner.hidden = true;
  });

  document.querySelectorAll('.wish-btn').forEach(btn => {
    btn.addEventListener('click', async (e) => {
      e.preventDefault();
      e.stopPropagation();
      const productId = btn.getAttribute('data-product-id');
      const body = new URLSearchParams();
      body.set('productId', productId);
      body.set('__RequestVerificationToken', antiforgeryToken());
      try {
        const res = await fetch('/Wishlist/Toggle', {
          method: 'POST',
          headers: { 'X-Requested-With': 'XMLHttpRequest', 'Content-Type': 'application/x-www-form-urlencoded' },
          body
        });
        const data = await res.json();
        toast(data.message, !data.success);
        if (data.success) {
          btn.classList.toggle('active');
          const el = document.getElementById('wishCount');
          if (el) el.textContent = data.count ?? 0;
        }
      } catch {
        toast('Could not update wishlist', true);
      }
    });
  });

  // Quick view
  document.querySelectorAll('.quick-view-btn').forEach(btn => {
    btn.addEventListener('click', async () => {
      const id = btn.getAttribute('data-id');
      const res = await fetch(`/product/quick-view/${id}`);
      if (!res.ok) {
        toast('Unable to load quick view', true);
        return;
      }
      const html = await res.text();
      const content = document.getElementById('quickViewContent');
      if (content) content.innerHTML = html;
      const modal = bootstrap.Modal.getOrCreateInstance(document.getElementById('quickViewModal'));
      modal.show();
    });
  });

  // Product details interactions
  const sizeInput = document.getElementById('selectedSizeId');
  const sizeFeedback = document.getElementById('sizeFeedback');
  const addBtn = document.getElementById('addToCartBtn');
  const buyBtn = document.getElementById('buyNowBtn');
  const qtyInput = document.getElementById('quantity');
  const whatsappBtn = document.getElementById('whatsappOrderBtn');
  let selectedLabel = '';

  function updateWhatsAppLink() {
    if (!whatsappBtn || !window.bareeraProduct) return;
    const qty = qtyInput?.value || 1;
    const sizeId = sizeInput?.value;
    let url = window.bareeraProduct.whatsappBase + `?quantity=${qty}`;
    if (sizeId) url += `&sizeId=${sizeId}`;
    whatsappBtn.setAttribute('href', url);
  }

  document.querySelectorAll('.size-option').forEach(btn => {
    btn.addEventListener('click', () => {
      document.querySelectorAll('.size-option').forEach(b => b.classList.remove('selected'));
      btn.classList.add('selected');
      sizeInput.value = btn.dataset.sizeId;
      selectedLabel = btn.dataset.sizeLabel || '';
      const stock = parseInt(btn.dataset.stock || '0', 10);
      if (qtyInput) qtyInput.max = stock;
      if (addBtn) addBtn.disabled = false;
      if (buyBtn) buyBtn.disabled = false;
      if (sizeFeedback) {
        sizeFeedback.textContent = `Selected: ${selectedLabel}`;
        sizeFeedback.classList.remove('error');
      }
      updateWhatsAppLink();
    });
  });

  document.querySelectorAll('.qty-btn').forEach(btn => {
    btn.addEventListener('click', () => {
      if (!qtyInput) return;
      const delta = parseInt(btn.dataset.delta || '0', 10);
      const max = parseInt(qtyInput.max || '99', 10);
      const next = Math.min(max, Math.max(1, (parseInt(qtyInput.value || '1', 10) + delta)));
      qtyInput.value = next;
      updateWhatsAppLink();
    });
  });
  qtyInput?.addEventListener('change', updateWhatsAppLink);

  document.querySelectorAll('.thumb').forEach(thumb => {
    thumb.addEventListener('click', () => {
      const src = thumb.dataset.src;
      const main = document.getElementById('mainProductImage');
      if (main && src) main.src = src;
      document.querySelectorAll('.thumb').forEach(t => t.classList.remove('active'));
      thumb.classList.add('active');
    });
  });

  const addForm = document.getElementById('addToCartForm');
  addForm?.addEventListener('submit', async (e) => {
    if (!sizeInput?.value) {
      e.preventDefault();
      if (sizeFeedback) {
        sizeFeedback.textContent = 'Please select a size before adding to cart.';
        sizeFeedback.classList.add('error');
      }
      toast('Please select a size', true);
      return;
    }

    const submitter = e.submitter;
    const isAjax = !submitter || submitter.id === 'addToCartBtn';
    if (!isAjax) return; // buy now: normal post then redirect handled server-side via cart

    e.preventDefault();
    const btn = addBtn;
    if (btn) {
      btn.disabled = true;
      btn.classList.add('loading');
      btn.textContent = 'Adding…';
    }
    try {
      const formData = new FormData(addForm);
      const res = await fetch(addForm.action, {
        method: 'POST',
        headers: { 'X-Requested-With': 'XMLHttpRequest' },
        body: formData
      });
      const data = await res.json();
      toast(data.message, !data.success);
      if (data.success) {
        if (cartCountEl) cartCountEl.textContent = data.count ?? 0;
        btn?.classList.add('pulse');
      }
    } catch {
      toast('Could not add to cart', true);
    } finally {
      if (btn) {
        btn.disabled = !sizeInput.value;
        btn.classList.remove('loading');
        btn.textContent = 'Add to Cart';
      }
    }
  });

  // Buy now: after add, go to checkout — intercept when buy now clicked
  buyBtn?.addEventListener('click', async (e) => {
    if (!sizeInput?.value) {
      e.preventDefault();
      toast('Please select a size', true);
      return;
    }
    e.preventDefault();
    const formData = new FormData(addForm);
    buyBtn.disabled = true;
    buyBtn.textContent = 'Please wait…';
    try {
      const res = await fetch(addForm.action, {
        method: 'POST',
        headers: { 'X-Requested-With': 'XMLHttpRequest' },
        body: formData
      });
      const data = await res.json();
      if (data.success) {
        window.location.href = '/Checkout';
      } else {
        toast(data.message || 'Unable to continue', true);
        buyBtn.disabled = false;
        buyBtn.textContent = 'Buy Now';
      }
    } catch {
      toast('Unable to continue', true);
      buyBtn.disabled = false;
      buyBtn.textContent = 'Buy Now';
    }
  });

  const placeOrderBtn = document.getElementById('placeOrderBtn');
  placeOrderBtn?.closest('form')?.addEventListener('submit', () => {
    placeOrderBtn.disabled = true;
    placeOrderBtn.textContent = 'Placing order…';
  });

  // Flash toasts
  document.querySelectorAll('.flash-success').forEach(el => toast(el.textContent.trim(), false));
  document.querySelectorAll('.flash-error').forEach(el => toast(el.textContent.trim(), true));

  // Header search autocomplete
  const searchInput = document.getElementById('globalSearch');
  const suggestBox = document.getElementById('searchSuggest');
  let suggestTimer;
  searchInput?.addEventListener('input', () => {
    clearTimeout(suggestTimer);
    const q = searchInput.value.trim();
    if (q.length < 2) {
      if (suggestBox) { suggestBox.hidden = true; suggestBox.innerHTML = ''; }
      return;
    }
    suggestTimer = setTimeout(async () => {
      try {
        const res = await fetch(`/product/suggest?q=${encodeURIComponent(q)}`);
        const items = await res.json();
        if (!suggestBox) return;
        if (!items.length) {
          suggestBox.innerHTML = '<div class="muted" style="padding:.8rem">No matches</div>';
          suggestBox.hidden = false;
          return;
        }
        suggestBox.innerHTML = items.map(i => `
          <a href="/product/${i.slug}">
            <img src="${i.imageUrl}" alt="" />
            <span><strong>${i.name}</strong><small>${i.categoryName} · Rs. ${Number(i.price).toLocaleString()}</small></span>
          </a>`).join('');
        suggestBox.hidden = false;
      } catch { /* ignore */ }
    }, 220);
  });
  document.addEventListener('click', (e) => {
    if (!suggestBox || !searchInput) return;
    if (!suggestBox.contains(e.target) && e.target !== searchInput) {
      suggestBox.hidden = true;
    }
  });
  searchInput?.addEventListener('keydown', (e) => {
    if (e.key === 'Enter') {
      e.preventDefault();
      const q = searchInput.value.trim();
      if (q) window.location.href = `/shop?search=${encodeURIComponent(q)}`;
    }
  });
})();
