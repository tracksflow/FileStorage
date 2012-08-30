Tracks Flow file storage
========================

Structure
---------

* client - C# client library for file storage
* server - file storage server on php language
* setup - nginx example config

Dependencies
------------

* nginx
* php-fpm

How to use
----------

Загрузка файла происходит порциями данных (чанками) — по одному POST-запросу на один чанк. Загрузка происходит последовательно. При обработке очередного запроса сервер дописывает полученную часть в результирующий файл. Все операции инвариантны по времени к длине целевого файла.
Все файлы хранятся на файловой системе в структуре каталогов:

`/<корневой_путь>/<scope>/<первые_два_символа_id>/<вторые_два_символа_id>/<пятый-последний_символ_id>/<filename.ext>`

При загрузке файла в целевой директории создаются временные файлы, которые затем удаляются.
Скачивание файлов происходит путем передачи в Nginx заголовка X-ACCEL_REDIRECT с путем к файлу. Отгрузкой занимается сам Nginx.

How to set up
-------------

* Ставим Nginx + php-fpm.
* В nginx создаем конфиг для хоста системы хранения примерно следующего содержания:

    	server {
    	listen		<your_LAN_ip>;
    	server_name		<you_file_storage_hostname>;
    
    	location /<location_name> {
    		internal;
    		root		<storage_root_path_should_be_the_same_in_lib.php>;
    	}
    
    	location ~ \.php {
    		root		/<path_to_php_code>;
    		fastcgi_pass	127.0.0.1:9000;
    		fastcgi_index	index.php;
    		fastcgi_param	SCRIPT_FILENAME /<path_to_php_code>/$fastcgi_script_name;
    		include		fastcgi_params;
    	}
     	}

* Копируем файлы сервера в директорию `<path_to_php_code>`
* Проверяем lib.php – функция getfsroot() должна возвращать путь, указанный в конфиге в `<storage_root_path_should_be_the_same_in_lib.php>`; функция `getlocroot()` должна содержать то же, что указано в конфиге в `<location_name>`.
* Ставим права на `<storage_root_path_should_be_the_same_in_lib.php>` те же, что и у Nginx / php-fpm, даем право записи.
* Если мы хотим использовать чанки больше 512КБайт, то надо прописать в php-fpm, nginx и php.ini соответствующие значения для ограничений на аплоад.