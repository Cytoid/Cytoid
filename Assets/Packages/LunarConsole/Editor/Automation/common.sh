pushd $(dirname $BASH_SOURCE[0]) >/dev/null
LUNAR_PLUGIN_DIR=$(cd "../.."; pwd)
popd >/dev/null

function epic_fail() {
    MESSAGE=$1
    echo ""
    echo "\033[31m"
    echo " ####################################### "
    echo "#########################################"
    echo "#########################################"
    echo "############  #############  ############"
    echo "#########################################"
    echo "#########################################"
    echo "#########################################"
    echo "####                                 ####"
    echo "#### ### ### ### ### ### ### ### ### ####"
    echo "#### ### ### ### ### ### ### ### ### ####"
    echo "####                                 ####"
    echo "#### ### ### ### ### ### ### ### ### ####"
    echo "#### ### ### ### ### ### ### ### ### ####"
    echo "####                                 ####"
    echo "#########################################"
    echo " ####################################### "
    echo "$MESSAGE"
    echo "\033[0m"
    echo ""
    echo ""
    echo ""
    echo ""
    if [ -z "$2" ] ; then
        exit $2
    fi

    exit 1
}

function exit_if_failed() {
    LAST_COMMAND_EXIT_CODE=$?
    MESSAGE=$1
    if [[ $LAST_COMMAND_EXIT_CODE != 0 ]] ; then
        epic_fail "$MESSAGE" $LAST_COMMAND_EXIT_CODE
    fi
}

function fail_if_file_not_exists() {
    file="$1"
    echo $file
    [ -f $file ] || epic_fail "File does not exist: $file"
}

function push_dir() {
    dir_target="$1"
    pushd $dir_target > /dev/null
    exit_if_failed "Can't push dir: $dir_target"
}

function pop_dir() {
    popd > /dev/null
    exit_if_failed "Can't pop dir"
}

LUNAR_SCRIPT_FILE="$LUNAR_PLUGIN_DIR/Scripts/LunarConsole.cs"
fail_if_file_not_exists $LUNAR_CONSOLE_FILE

function replace_text_in_file() {

    file="$1"
    fail_if_file_not_exists $file

    text=$2
    replacement=$3

    sed -i "" "s/${text}/${replacement}/g" $file || epic_fail "Can't update script file: $file"
}

function disable_plugin() {
    replace_text_in_file $LUNAR_SCRIPT_FILE "#define LUNAR_CONSOLE_ENABLED" "#define LUNAR_CONSOLE_DISABLED"
}

function enable_plugin() {
    replace_text_in_file $LUNAR_SCRIPT_FILE "#define LUNAR_CONSOLE_DISABLED" "#define LUNAR_CONSOLE_ENABLED"
}