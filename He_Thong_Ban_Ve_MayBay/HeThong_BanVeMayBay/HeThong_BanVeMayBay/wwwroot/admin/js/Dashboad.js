let revenueChart = null;

document.addEventListener('DOMContentLoaded', function() {
    initChart();
    
    $('#yearSelector').on('change', function() {
        updateChartData(this.value);
    });
    
    // Khởi tạo ban đầu với năm hiện tại
    updateChartData($('#yearSelector').val());
});

function initChart() {
    const ctx = document.getElementById('revenueChart').getContext('2d');
    const labels = ['Th1', 'Th2', 'Th3', 'Th4', 'Th5', 'Th6', 'Th7', 'Th8', 'Th9', 'Th10', 'Th11', 'Th12'];
    
    revenueChart = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: labels,
            datasets: [{
                label: 'Doanh thu (VND)',
                data: Array(12).fill(0),
                backgroundColor: (context) => {
                    const month = new Date().getMonth();
                    const currentYear = new Date().getFullYear();
                    const selectedYear = parseInt($('#yearSelector').val());
                    
                    if (selectedYear === currentYear && context.dataIndex === month) {
                        return '#feba02'; // Highlight tháng hiện tại của năm hiện tại
                    }
                    return '#003580';
                },
                borderRadius: 8
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: { 
                legend: { display: false },
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            return context.raw.toLocaleString() + ' VND';
                        }
                    }
                }
            },
            scales: {
                y: { 
                    beginAtZero: true,
                    ticks: {
                        callback: function(value) {
                            if (value >= 1000000) return (value / 1000000) + 'M';
                            return value.toLocaleString();
                        }
                    }
                },
                x: { 
                    grid: { display: false }
                }
            }
        }
    });
}

async function updateChartData(year) {
    try {
        const response = await fetch(`/Admin/Home/GetRevenueData?year=${year}`);
        const result = await response.json();
        
        if (result.success && revenueChart) {
            const maxVal = Math.max(...result.data);
            revenueChart.data.datasets[0].data = result.data;
            
            revenueChart.options.scales.y.suggestedMax = maxVal > 0 ? maxVal * 1.2 : 1000000;
            
            revenueChart.update();
        }
    } catch (error) {
        console.error('Error fetching revenue data:', error);
    }
}