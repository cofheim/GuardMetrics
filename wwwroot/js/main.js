document.addEventListener('DOMContentLoaded', function() {
    // Активация всплывающих подсказок Bootstrap
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.map(function(tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });

    // Анимация элементов при прокрутке
    const animateOnScroll = function() {
        const elements = document.querySelectorAll('.feature-card, .timeline-item, .dashboard-card');
        
        elements.forEach(element => {
            const position = element.getBoundingClientRect();
            
            // Проверяем, виден ли элемент в viewport
            if(position.top < window.innerHeight && position.bottom >= 0) {
                element.classList.add('animated');
            }
        });
    };

    // Вызываем функцию при загрузке страницы и при прокрутке
    window.addEventListener('scroll', animateOnScroll);
    animateOnScroll();

    // Обработка формы подписки (предотвращаем отправку)
    const subscribeForm = document.querySelector('footer form');
    if (subscribeForm) {
        subscribeForm.addEventListener('submit', function(e) {
            e.preventDefault();
            const emailInput = this.querySelector('input[type="email"]');
            
            if (emailInput && emailInput.value) {
                // Добавляем уведомление об успешной подписке
                const alert = document.createElement('div');
                alert.className = 'alert alert-success mt-3';
                alert.role = 'alert';
                alert.textContent = 'Спасибо за подписку! Вы будете получать новости о GuardMetrics.';
                this.appendChild(alert);
                
                // Очищаем поле ввода
                emailInput.value = '';
                
                // Удаляем уведомление через 5 секунд
                setTimeout(() => {
                    alert.remove();
                }, 5000);
            }
        });
    }

    // Добавляем обработку для API-вызовов (заглушка для демо)
    const apiCallButtons = document.querySelectorAll('[data-api-call]');
    apiCallButtons.forEach(button => {
        button.addEventListener('click', function() {
            const endpoint = this.getAttribute('data-api-call');
            
            // Показываем загрузку
            this.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Загрузка...';
            this.disabled = true;
            
            // Симулируем API-вызов
            setTimeout(() => {
                this.innerHTML = this.getAttribute('data-original-text') || 'Готово';
                this.disabled = false;
                
                // Показываем уведомление об успехе
                const container = document.querySelector('.api-response') || document.createElement('div');
                container.className = 'alert alert-info mt-3 api-response';
                container.textContent = `API-вызов к ${endpoint} выполнен успешно!`;
                
                if (!document.querySelector('.api-response')) {
                    this.parentNode.appendChild(container);
                }
            }, 1500);
        });
    });

    // Функция для получения метрик (использовать с реальным API)
    window.fetchMetrics = async function(metricType, timeRange) {
        try {
            // В реальном проекте здесь должен быть вызов к API
            // const response = await fetch(`/api/metrics/${metricType}?timeRange=${timeRange}`);
            // const data = await response.json();
            
            // Заглушка для демонстрации
            const demoData = {
                'process': [
                    { timestamp: '2025-03-20T10:00:00', value: 35 },
                    { timestamp: '2025-03-20T11:00:00', value: 42 },
                    { timestamp: '2025-03-20T12:00:00', value: 38 },
                    { timestamp: '2025-03-20T13:00:00', value: 45 },
                    { timestamp: '2025-03-20T14:00:00', value: 50 }
                ],
                'network': [
                    { timestamp: '2025-03-20T10:00:00', value: 120 },
                    { timestamp: '2025-03-20T11:00:00', value: 145 },
                    { timestamp: '2025-03-20T12:00:00', value: 165 },
                    { timestamp: '2025-03-20T13:00:00', value: 132 },
                    { timestamp: '2025-03-20T14:00:00', value: 158 }
                ],
                'security': [
                    { timestamp: '2025-03-20T10:00:00', value: 2 },
                    { timestamp: '2025-03-20T11:00:00', value: 0 },
                    { timestamp: '2025-03-20T12:00:00', value: 1 },
                    { timestamp: '2025-03-20T13:00:00', value: 0 },
                    { timestamp: '2025-03-20T14:00:00', value: 3 }
                ]
            };
            
            return demoData[metricType] || [];
        } catch (error) {
            console.error('Ошибка при получении метрик:', error);
            return [];
        }
    };
}); 