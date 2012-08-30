<?php

include_once('lib.php');
include_once('mime.php');

function checkerror($errorcode)
{
	if ($errorcode == 0) {
		//no error
	} else if ($errorcode == 1) {
		http_response_code(500);
		echo 'Error while creating file or directory.';
		exit();
	} else if ($errorcode == 2) {
		http_response_code(500);
		echo 'Error while opening file or directory.';
		exit();
	} else if ($errorcode == 3) {
		http_response_code(500);
		echo 'Error while renaming file or directory.';
		exit();
	} else if ($errorcode == 4) {
		http_response_code(500);
		echo 'Error while saving input file.';
		exit();
	} else if ($errorcode == 5) {
		http_response_code(500);
		echo 'Error while deleting file or directory.';
		exit();
	} else if ($errorcode == 6) {
		http_response_code(404);
		echo 'Error: no source file or directory.';
		exit();
	} else if ($errorcode == 7) {
		http_response_code(500);
		echo 'Error: no source metadata file.';
		exit();
	} else {
		http_response_code(500);
		echo 'Unknown error.';
		exit();
	}
}

function getpath()
{
	$scope = $_REQUEST['scope'];
	$fname = $_REQUEST['fname'];
	$id = $_REQUEST['id'];

	return getfspath($scope, $fname, $id);
}

if (!array_key_exists('action', $_REQUEST))
{
	http_response_code(500);
	echo 'invalid request';
}
else if ($_REQUEST['action'] == "put")
{
	$start = $_REQUEST['start'];
	$end = $_REQUEST['end'];
	$total = $_REQUEST['total'];

	$path = getpath();

	$res = extendfile($path, $end-$start+1, $total);
	checkerror($res);
	$res = savechunk($path, $start, $end);
	checkerror($res);
	$res = mergechunk($path, $start, $end);
	checkerror($res);
	$res = cleanchunk($path, $start, $end);
	checkerror($res);
}
else if ($_REQUEST['action'] == "get")
{
	$scope = $_REQUEST['scope'];
	$fname = $_REQUEST['fname'];
	$id = $_REQUEST['id'];
	$ext = substr(strrchr($fname, '.'), 1);

	header('Content-Disposition: attachment; filename='.$fname);
	header('Content-Type: ' . getmimetype($ext));

	$path = getlocpath($scope, $fname, $id);
	header('X-ACCEL-REDIRECT: ' . $path);
}
else if ($_REQUEST['action'] == "del")
{
	$path = getpath();

	if (file_exists($path))
	{
		if (unlink($path))
		{
			//ok
		}
		else
		{
			checkerror(5);
		}
	}
	else
	{
		checkerror(6);
	}
}
else if ($_REQUEST['action'] == "size")
{
	$path = getpath();

	if (file_exists($path))
	{
		$statarr = stat($path);
		echo $statarr["size"];
	}
	else
	{
		checkerror(6);
	}
}
else if ($_REQUEST['action'] == "exists")
{
	$path = getpath();

	if (file_exists($path))
	{
		//yes
	}
	else
	{
		checkerror(6);
	}
}
else if ($_REQUEST['action'] == "md5")
{
	$path = getpath();

	if (file_exists($path))
	{
		echo substr(shell_exec('md5sum ' . $path), 0, 32);
	}
	else
	{
		checkerror(6);
	}
}
else
{
	http_response_code(500);
	echo 'unknown method';
}



?>

