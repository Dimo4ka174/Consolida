document.addEventListener('DOMContentLoaded', function () {
    const fileInput = document.getElementById('fileInput');
    const fileLabel = document.getElementById('fileLabel');

    fileInput.addEventListener('change', function () {
        if (this.files.length) {
            const selectedFile = this.files[0];
            fileLabel.classList.add('selected');
            fileLabel.textContent = selectedFile.name;
        } else {
            fileLabel.classList.remove('selected');
            fileLabel.textContent = 'Выбрать файл';
        }
    });

    function validateFileUpload() {
        if (!fileInput.files.length) {
            alert('Файл не выбран.');
            fileLabel.textContent = 'Выбрать файл';
            return false;
        }

        const selectedFile = fileInput.files[0];
        if (!selectedFile.name.endsWith('.xlsx')) {
            alert('Разрешены только .xlsx файлы.');
            fileInput.value = '';
            fileLabel.textContent = 'Выбрать файл';
            return false;
        }

        fileLabel.textContent = selectedFile.name;
        return true;
    }

    document.querySelector('form').addEventListener('submit', function (e) {
        if (!validateFileUpload()) {
            e.preventDefault();
        }
    });
});
