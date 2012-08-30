<?php
function getmimetype($ext){
	$res = '';
	switch ($ext) {
		case 'gif' :
			$res = 'image/gif';
			break;
		case 'jpg' :
			$res = 'image/jpeg';
			break;
		case 'jpeg' :
			$res = 'image/jpeg';
			break;
		case 'png' :
			$res = 'image/png';
			break;
		case 'bmp' :
			$res = 'image/x-ms-bmp';
			break;
		case 'rar' :
			$res = 'application/x-rar-compressed';
			break;
		case 'zip' :
			$res = 'application/zip';
			break;
		case 'swf' :
			$res = 'application/x-shockwave-flash';
			break;
		case 'mp3' :
			$res = 'audio/mpeg';
			break;
		case 'wav' :
			$res = 'audio/wav';
			break;
		case 'flac' :
			$res = 'audio/flac';
			break;
		case 'alac' :
			$res = 'audio/x-m4a';
			break;
		case '3gp' :
			$res = 'video/3gpp';
			break;
		case 'mpg' :
			$res = 'video/mpeg';
			break;
		case 'mpeg' :
			$res = 'video/mpeg';
			break;
		case 'mp4' :
			$res = 'video/mp4';
			break;
		case 'mov' :
			$res = 'video/quicktime';
			break;
		case 'flv' :
			$res = 'video/x-flv';
			break;
		case 'f4v' :
			$res = 'video/mp4';
			break;
		case 'wmv' :
			$res = 'video/x-ms-wmv';
			break;
		case 'avi' :
			$res = 'video/x-msvideo';
			break;
		default :
			$res = 'application/octet-stream';
	}

	return $res;
}
?>