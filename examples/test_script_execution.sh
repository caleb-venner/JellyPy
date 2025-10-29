#!/bin/bash

# JellyPy Script Execution Test Script (Bash Version)
#
# This script is designed to test and debug script execution functionality
# in a shell/bash environment. It logs all received data and verifies that
# the script execution system is passing data correctly.
#
# Usage:
# 1. Configure this script path in JellyPy Script Settings
# 2. Set the executor type to Bash
# 3. Set various event triggers (PlaybackStart, ItemAdded, etc.)
# 4. Configure data attributes to pass event information
# 5. Trigger events in Jellyfin to test
# 6. Check the log files to verify output

set -o pipefail

# Setup logging
LOG_DIR="/tmp/jellypy_test"
LOG_FILE="${LOG_DIR}/test_bash_execution.log"
mkdir -p "$LOG_DIR"

# Redirect output to log file and stderr
exec 1> >(tee -a "$LOG_FILE")
exec 2> >(tee -a "$LOG_FILE" >&2)

# Helper function to print section separators
print_separator() {
    local title="$1"
    echo "======================================================================"
    echo " $title"
    echo "======================================================================"
}

# Helper function to log with timestamp
log_info() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] INFO: $1"
}

log_debug() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] DEBUG: $1"
}

log_warning() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] WARNING: $1"
}

log_error() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] ERROR: $1"
}

# Main execution starts here
main() {
    log_info "JellyPy Bash Script Execution Test Started"
    log_info "Bash version: $BASH_VERSION"
    log_info "Log file: $LOG_FILE"

    # Log environment
    print_separator "ENVIRONMENT INFORMATION"
    log_info "Hostname: $(hostname 2>/dev/null || echo 'unknown')"
    log_info "Current user: $(whoami)"
    log_info "Working directory: $(pwd)"
    log_info "Shell: $SHELL"
    log_info "Process ID: $$"
    log_info "Script path: $0"

    # Log script arguments
    print_separator "COMMAND LINE ARGUMENTS"
    log_info "Total arguments: $#"
    local arg_num=0
    for arg in "$@"; do
        ((arg_num++))
        # Truncate long arguments for readability
        if [ ${#arg} -gt 200 ]; then
            log_info "Argument $arg_num: ${arg:0:200}... (truncated)"
        else
            log_info "Argument $arg_num: $arg"
        fi
    done

    # Log environment variables
    print_separator "ENVIRONMENT VARIABLES"
    log_info "Total environment variables: $(env | wc -l)"
    
    # Log all variables sorted
    env | sort | while read -r line; do
        # Truncate long values for readability
        if [ ${#line} -gt 200 ]; then
            log_debug "${line:0:200}... (truncated)"
        else
            log_debug "$line"
        fi
    done

    # Extract and log JellyPy-specific variables
    print_separator "JELLYPY EVENT DATA"

    local event_type="${EVENT_TYPE:-unknown}"
    local user_name="${USER_NAME:-unknown}"
    local item_name="${ITEM_NAME:-unknown}"
    local item_type="${ITEM_TYPE:-unknown}"
    local timestamp="${TIMESTAMP:-unknown}"
    local client_name="${CLIENT_NAME:-}"
    local series_name="${SERIES_NAME:-}"
    local season_num="${SEASON_NUMBER:-}"
    local episode_num="${EPISODE_NUMBER:-}"

    log_info "EVENT_TYPE: $event_type"
    log_info "TIMESTAMP: $timestamp"
    log_info "USER_NAME: $user_name"
    log_info "ITEM_NAME: $item_name"
    log_info "ITEM_TYPE: $item_type"
    log_info "CLIENT_NAME: $client_name"
    log_info "SERIES_NAME: $series_name"
    log_info "SEASON_NUMBER: $season_num"
    log_info "EPISODE_NUMBER: $episode_num"

    # Validate event data
    print_separator "EVENT DATA VALIDATION"
    validate_event_data

    # Generate summary
    print_separator "EXECUTION SUMMARY"
    generate_summary

    log_info "JellyPy Bash Script Execution Test Completed Successfully"
    return 0
}

# Function to validate event data
validate_event_data() {
    local validation_passed=0
    local validation_total=0

    # Check for required EVENT_TYPE
    ((validation_total++))
    if [ -n "$EVENT_TYPE" ] && [ "$EVENT_TYPE" != "unknown" ]; then
        log_info "✓ EVENT_TYPE is present: $EVENT_TYPE"
        ((validation_passed++))
    else
        log_warning "✗ EVENT_TYPE is missing or invalid"
    fi

    # Check for required TIMESTAMP
    ((validation_total++))
    if [ -n "$TIMESTAMP" ] && [ "$TIMESTAMP" != "unknown" ]; then
        log_info "✓ TIMESTAMP is present: $TIMESTAMP"
        ((validation_passed++))
    else
        log_warning "✗ TIMESTAMP is missing or invalid"
    fi

    # Validate event type against known types
    ((validation_total++))
    case "$EVENT_TYPE" in
        PlaybackStart|PlaybackStop|PlaybackPause|PlaybackResume|\
        ItemAdded|ItemUpdated|ItemRemoved|\
        UserCreated|UserUpdated|UserDeleted|\
        SessionStart|SessionEnd|\
        ServerStartup|ServerShutdown)
            log_info "✓ EVENT_TYPE is valid: $EVENT_TYPE"
            ((validation_passed++))
            ;;
        *)
            log_warning "✗ Unknown EVENT_TYPE: $EVENT_TYPE"
            ;;
    esac

    # Check for item data on playback/item events
    ((validation_total++))
    if [[ "$EVENT_TYPE" =~ Playback|Item ]]; then
        if [ -n "$ITEM_NAME" ] && [ "$ITEM_NAME" != "unknown" ]; then
            log_info "✓ ITEM_NAME is present for $EVENT_TYPE event: $ITEM_NAME"
            ((validation_passed++))
        else
            log_warning "✗ ITEM_NAME is missing for $EVENT_TYPE event"
        fi
    else
        ((validation_passed++))
    fi

    log_info "Validation result: $validation_passed/$validation_total checks passed"
}

# Function to generate summary report
generate_summary() {
    local summary_file="${LOG_DIR}/summary_bash_$(date '+%Y%m%d_%H%M%S').txt"

    {
        echo "JellyPy Bash Script Execution Summary"
        echo "======================================"
        echo "Execution timestamp: $(date '+%Y-%m-%d %H:%M:%S')"
        echo "Event type: ${EVENT_TYPE:-unknown}"
        echo "User: ${USER_NAME:-unknown}"
        echo "Item: ${ITEM_NAME:-unknown}"
        echo "Item type: ${ITEM_TYPE:-unknown}"
        echo "Script path: $0"
        echo "Working directory: $(pwd)"
        echo "Environment vars count: $(env | wc -l)"
        echo "Command args count: $#"
        echo "Log file: $LOG_FILE"
    } > "$summary_file"

    log_info "Summary written to: $summary_file"
    cat "$summary_file"
}

# Error handler
trap 'log_error "Script execution failed"; exit 1' ERR

# Run main function
main "$@"
exit $?
