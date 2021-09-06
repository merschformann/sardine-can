#!/bin/bash

version_file="../../VERSION.txt"
projects=$( find ../../ -name "*.csproj" )

for project in $projects
do
	echo "Checking ${project} ..."
	case `grep -f ${version_file} ${project} >/dev/null; echo $?` in
		0)
			# code if found
			echo "Versions are consistent - SUCCESS"
			;;
		1)
			# code if not found
			echo "Version in ${project} does not match the one in ${version_file} - ERROR"
			exit 1
			;;
		*)
			# code if an error occurred
			echo "Error occurred while validating version consistency - ERROR"
			exit -1
			;;
	esac
done



