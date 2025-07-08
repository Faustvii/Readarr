import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import * as commandNames from 'Commands/commandNames';
import withCurrentPage from 'Components/withCurrentPage';
import { fetchBooksByIds } from 'Store/Actions/bookActions';
import { executeCommand } from 'Store/Actions/commandActions';
import * as queueActions from 'Store/Actions/queueActions';
import createCommandExecutingSelector from 'Store/Selectors/createCommandExecutingSelector';
import { registerPagePopulator, unregisterPagePopulator } from 'Utilities/pagePopulator';
import Queue from './Queue';

function createMapStateToProps() {
  return createSelector(
    (state) => state.authors,
    (state) => state.books,
    (state) => state.queue.options,
    (state) => state.queue.paged,
    createCommandExecutingSelector(commandNames.REFRESH_MONITORED_DOWNLOADS),
    (authors, books, options, queue, isRefreshMonitoredDownloadsExecuting) => {
      return {
        isAuthorFetching: authors.isFetching,
        isAuthorPopulated: authors.isPopulated,
        isBooksFetching: books.isFetching,
        isBooksPopulated: books.isPopulated,
        booksError: books.error,
        books: books.items,
        isRefreshMonitoredDownloadsExecuting,
        ...options,
        ...queue
      };
    }
  );
}

const mapDispatchToProps = {
  ...queueActions,
  fetchBooksByIds,
  executeCommand
};

class QueueConnector extends Component {

  constructor(props) {
    super(props);
    this.state = {
      fetchedBookIds: new Set()
    };
  }

  //
  // Lifecycle

  componentDidMount() {
    const {
      useCurrentPage,
      fetchQueue,
      fetchQueueStatus,
      gotoQueueFirstPage
    } = this.props;

    registerPagePopulator(this.repopulate);

    if (useCurrentPage) {
      fetchQueue();
    } else {
      gotoQueueFirstPage();
    }

    fetchQueueStatus();
  }

  componentDidUpdate(prevProps) {
    if (this.props.items && this.props.items.length > 0 && !this.props.isBooksFetching) {
      const bookIds = this.props.items
        .map((item) => item.bookId)
        .filter((bookId) => bookId != null)
        .filter((bookId, index, array) => array.indexOf(bookId) === index);
      const currentBooks = this.props.books || [];
      const missingBookIds = bookIds.filter((bookId) =>
        !currentBooks.some((book) => book.id === bookId)
      );
      const newBookIds = missingBookIds.filter((bookId) => !this.state.fetchedBookIds.has(bookId));
      if (newBookIds.length > 0) {
        this.setState((prevState) => ({
          fetchedBookIds: new Set([...prevState.fetchedBookIds, ...newBookIds])
        }));
        this.props.fetchBooksByIds({ bookIds: newBookIds });
      }
    }
    if (
      this.props.includeUnknownAuthorItems !==
      prevProps.includeUnknownAuthorItems
    ) {
      this.repopulate();
    }
  }

  componentWillUnmount() {
    unregisterPagePopulator(this.repopulate);
  }

  //
  // Control

  repopulate = () => {
    this.props.fetchQueue();
  };

  //
  // Listeners

  onFirstPagePress = () => {
    this.props.gotoQueueFirstPage();
  };

  onPreviousPagePress = () => {
    this.props.gotoQueuePreviousPage();
  };

  onNextPagePress = () => {
    this.props.gotoQueueNextPage();
  };

  onLastPagePress = () => {
    this.props.gotoQueueLastPage();
  };

  onPageSelect = (page) => {
    this.props.gotoQueuePage({ page });
  };

  onSortPress = (sortKey) => {
    this.props.setQueueSort({ sortKey });
  };

  onTableOptionChange = (payload) => {
    this.props.setQueueTableOption(payload);

    if (payload.pageSize) {
      this.props.gotoQueueFirstPage();
    }
  };

  onRefreshPress = () => {
    this.props.executeCommand({
      name: commandNames.REFRESH_MONITORED_DOWNLOADS
    });
  };

  onGrabSelectedPress = (ids) => {
    this.props.grabQueueItems({ ids });
  };

  onRemoveSelectedPress = (payload) => {
    this.props.removeQueueItems(payload);
  };

  //
  // Render

  render() {
    return (
      <Queue
        onFirstPagePress={this.onFirstPagePress}
        onPreviousPagePress={this.onPreviousPagePress}
        onNextPagePress={this.onNextPagePress}
        onLastPagePress={this.onLastPagePress}
        onPageSelect={this.onPageSelect}
        onSortPress={this.onSortPress}
        onTableOptionChange={this.onTableOptionChange}
        onRefreshPress={this.onRefreshPress}
        onGrabSelectedPress={this.onGrabSelectedPress}
        onRemoveSelectedPress={this.onRemoveSelectedPress}
        {...this.props}
      />
    );
  }
}

QueueConnector.propTypes = {
  useCurrentPage: PropTypes.bool.isRequired,
  items: PropTypes.arrayOf(PropTypes.object).isRequired,
  includeUnknownAuthorItems: PropTypes.bool.isRequired,
  fetchQueue: PropTypes.func.isRequired,
  fetchQueueStatus: PropTypes.func.isRequired,
  gotoQueueFirstPage: PropTypes.func.isRequired,
  gotoQueuePreviousPage: PropTypes.func.isRequired,
  gotoQueueNextPage: PropTypes.func.isRequired,
  gotoQueueLastPage: PropTypes.func.isRequired,
  gotoQueuePage: PropTypes.func.isRequired,
  setQueueSort: PropTypes.func.isRequired,
  setQueueTableOption: PropTypes.func.isRequired,
  clearQueue: PropTypes.func.isRequired,
  grabQueueItems: PropTypes.func.isRequired,
  removeQueueItems: PropTypes.func.isRequired,
  fetchBooksByIds: PropTypes.func.isRequired,
  executeCommand: PropTypes.func.isRequired,
  isBooksFetching: PropTypes.bool.isRequired,
  books: PropTypes.arrayOf(PropTypes.object).isRequired
};

export default withCurrentPage(
  connect(createMapStateToProps, mapDispatchToProps)(QueueConnector)
);
