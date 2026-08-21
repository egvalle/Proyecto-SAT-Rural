const menuToggle = document.querySelector('.menu-toggle');
const mainNav = document.querySelector('#main-nav');
const newProjectButton = document.querySelector('#new-project-button');
const toast = document.querySelector('#toast');

menuToggle.addEventListener('click', () => {
  const isOpen = mainNav.classList.toggle('open');
  menuToggle.setAttribute('aria-expanded', String(isOpen));
  menuToggle.setAttribute('aria-label', isOpen ? 'Cerrar menu' : 'Abrir menu');
});

document.querySelectorAll('.nav-link').forEach((link) => {
  link.addEventListener('click', () => {
    mainNav.classList.remove('open');
    menuToggle.setAttribute('aria-expanded', 'false');
    menuToggle.setAttribute('aria-label', 'Abrir menu');
    document.querySelectorAll('.nav-link').forEach((item) => item.classList.remove('active'));
    link.classList.add('active');
  });
});

newProjectButton.addEventListener('click', () => {
  toast.classList.add('visible');
  window.setTimeout(() => toast.classList.remove('visible'), 3000);
});
