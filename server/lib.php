<?php

/* Settings start */
//This function returns file storage folder root path
function getfsroot()
{
	return '/opt/storage';
}
//This function returns root path for X-ACCEL-REDIRECT responses
function getlocroot()
{
	return '/storage';
}

/* Settings end */

/* Methods start */

function createdirs($path, $isdirpath)
{
	$dir = '/' . strtok($path, '/');
	$tok = strtok('/');

	while($tok !== FALSE)
	{
		if (!is_dir($dir)) {
			if (!mkdir($dir)) {
				return 1;
			}
		}
		$dir = $dir . '/' . $tok;
		$tok = strtok('/');
	}
	
	if ($isdirpath)
	{
		if (!mkdir($dir)) {
			return 1;
		}
	}

	return 0;
}

function getchunkpath($path, $start, $end)
{
	return $path . '_' . $start . '-' . $end;
}

function extendfile($path, $len, $maxlen)
{
	$currlen=0;
	if (file_exists($path)){
		$currlen = filesize($path);
	}

	if ($currlen < $maxlen)
	{
		$err = createdirs($path, 0);
		if ($err != 0) {
			return $err;
		}

		$zero = fopen('/dev/zero', 'r');
		if (!$zero) {
			return 2;
		}
		$dest = fopen($path, 'a');
		if (!$dest) {
			return 2;
		}

		stream_copy_to_stream($zero, $dest, min($len, $maxlen - $currlen));

		fclose($zero);
		fclose($dest);
	}

	return 0;
}

function mergechunk($path, $start, $end)
{
	createdirs($path, 0);

	$chunkpath = getchunkpath($path, $start, $end);
	$f = fopen($chunkpath, 'r');
	if (!$f) {
		return 2;
	}
	$data = fread($f, $end-$start+1);
	fclose($f);

	$f = fopen($path, 'r+');
	if (!$f) {
		return 2;
	}
	fseek($f, $start);
	fwrite($f, $data);
	fclose($f);

	$res = rename($chunkpath, $chunkpath . '_s');
	if (!$res) {
		return 3;
	}

	return 0;
}

function savechunk($path, $start, $end)
{
	if (filesize($_FILES['data']['tmp_name']) != $end-$start+1)
	{
		mylog('invalid file size: ' . filesize($_FILES['data']['tmp_name']) . '.', 1);
		return 4;
	}

	$chunkpath = getchunkpath($path, $start, $end);
	$res = createdirs($chunkpath, 0);
	if ($res != 0) {
		return $res;
	}
	if (!move_uploaded_file($_FILES['data']['tmp_name'], $chunkpath))
	{
		mylog('save chunk has failed', 1);
		return 4;
	}

	return 0;
}

function getchunkinfo($chunkpath)
{
	$synced = 0;
	if (strpos($chunkpath, '_s') > 0)
	{
		$synced = 1;
		$chunkpath = str_replace('_s', '', $chunkpath);
	}

	$tmp = strstr($chunkpath, '_');
	$tmp = str_replace('_', '', $tmp);
	$start = str_replace(strstr($tmp, '-'), '', $tmp);
	$end = str_replace('-', '', strstr($tmp, '-'));

	return array(
		'start' => $start,
		'end' => $end,
		'synced' => $synced
		);
}

function cleanchunk($path, $start, $end)
{
	foreach (glob(getchunkpath($path, $start, $end) . '*') as $filename) {
		if (!unlink($filename)) {
			return 5;
		}
	}

	return 0;
}

function movedir($frompath, $topath)
{
	if (!is_dir($frompath))
	{
		return 6;
	}

	createdirs($topath, 1);

	$fromdir = opendir($frompath);
	if (!$fromdir) {
		return 2;
	}
	$todir = opendir($topath);
	if (!$todir) {
		return 2;
	}

	while (false !== ($file = readdir($fromdir))) {
		if ($file != '.' && $file != '..'){
			$res = rename($frompath . '/' . $file, $topath . '/' . $file);
			if (!$res) {
				return 3;
			}
		}
	}

	closedir($fromdir); 
	closedir($todir);

	$res = rmdir($frompath);
	if (!$res) {
		return 5;
	}

	return 0;
}

function getpathinternal($scope, $fname, $id, $root)
{
	$scope = strtolower($scope);
	$fname = strtolower($fname);
	$id = strtolower($id);

	if (strlen($id) < 5)
	{
		$id = str_pad($id, 5, '0', STR_PAD_LEFT);
	}

	$path = $root . '/' . $scope . '/' . substr($id, 0, 2) . '/' . substr($id, 2, 2) . '/' . substr($id, 4);
	if ($fname)
	{
		$path = $path . '/' . $fname;
	}
	return $path;
}

function getfsdir($scope, $id)
{
	return getpathinternal($scope, '', $id, getfsroot());
}

function getfspath($scope, $fname, $id)
{
	return getpathinternal($scope, $fname, $id, getfsroot());
}

function getlocpath($scope, $fname, $id)
{
	return getpathinternal($scope, $fname, $id, getlocroot());
}

function mylog($message, $debug)
{
	if ($debug == 0){
		echo $message;
	}
}

/* Methods end */

?>