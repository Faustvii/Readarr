/* eslint max-params: 0 */
import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import * as commandNames from 'Commands/commandNames';
import { toggleAuthorMonitored } from 'Store/Actions/authorActions';
import { clearBooks, fetchBooksByAuthor, gotoBooksFirstPage, gotoBooksLastPage, gotoBooksNextPage, gotoBooksPage, gotoBooksPreviousPage } from 'Store/Actions/bookActions';
import { clearBookFiles, fetchBookFiles } from 'Store/Actions/bookFileActions';
import { saveBookEditor } from 'Store/Actions/bookIndexActions';
import { executeCommand } from 'Store/Actions/commandActions';
import { clearQueueDetails, fetchQueueDetails } from 'Store/Actions/queueActions';
import { cancelFetchReleases, clearReleases } from 'Store/Actions/releaseActions';
import { clearSeries, fetchSeries } from 'Store/Actions/seriesActions';
import createAllAuthorSelector from 'Store/Selectors/createAllAuthorsSelector';
import createCommandsSelector from 'Store/Selectors/createCommandsSelector';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import createSortedSectionSelector from 'Store/Selectors/createSortedSectionSelector';
import { findCommand, isCommandExecuting } from 'Utilities/Command';
import { registerPagePopulator, unregisterPagePopulator } from 'Utilities/pagePopulator';
import AuthorDetails from './AuthorDetails';

const selectBooks = createSelector(
  (state) => state.books,
  (state) => state.bookIndex,
  (state, { id }) => id,
  (books, index, authorId) => {
    const {
      items,
      isFetching,
      isPopulated,
      error,
      pageSize,
      totalPages,
      totalRecords,
      page
    } = books;

    const {
      isSaving,
      saveError,
      isDeleting,
      deleteError
    } = index;

    const authorBooks = authorId ? items.filter((book) => book.authorId === authorId) : items;
    const hasBooks = !!authorBooks.length;
    const hasMonitoredBooks = authorBooks.some((e) => e.monitored);

    return {
      isBooksFetching: isFetching,
      isBooksPopulated: isPopulated,
      booksError: error,
      hasBooks,
      hasMonitoredBooks,
      isSaving,
      saveError,
      isDeleting,
      deleteError,
      pageSize,
      totalPages,
      totalRecords,
      page
    };
  }
);

const selectSeries = createSelector(
  createSortedSectionSelector('series', (a, b) => a.title.localeCompare(b.title)),
  (state) => state.series,
  (series) => {
    const {
      items,
      isFetching,
      isPopulated,
      error
    } = series;

    const hasSeries = !!items.length;

    return {
      isSeriesFetching: isFetching,
      isSeriesPopulated: isPopulated,
      seriesError: error,
      hasSeries,
      series: series.items
    };
  }
);

const selectBookFiles = createSelector(
  (state) => state.bookFiles,
  (bookFiles) => {
    const {
      items,
      isFetching,
      isPopulated,
      error
    } = bookFiles;

    const hasBookFiles = !!items.length;

    return {
      isBookFilesFetching: isFetching,
      isBookFilesPopulated: isPopulated,
      bookFilesError: error,
      hasBookFiles
    };
  }
);

function createMapStateToProps() {
  return createSelector(
    (state, { titleSlug }) => titleSlug,
    selectBooks,
    selectSeries,
    selectBookFiles,
    createAllAuthorSelector(),
    createCommandsSelector(),
    createDimensionsSelector(),
    (state) => state.books,
    (titleSlug, books, series, bookFiles, allAuthors, commands, dimensions, rawBooks) => {
      const sortedAuthor = _.orderBy(allAuthors, 'sortNameLastFirst');
      const authorIndex = _.findIndex(sortedAuthor, { titleSlug });
      const author = sortedAuthor[authorIndex];

      if (!author) {
        return {};
      }

      const {
        isBooksFetching,
        isBooksPopulated,
        booksError,
        hasBooks,
        hasMonitoredBooks,
        isSaving,
        saveError,
        isDeleting,
        deleteError,
        pageSize,
        totalPages,
        totalRecords,
        page
      } = books;

      const {
        isSeriesFetching,
        isSeriesPopulated,
        seriesError,
        hasSeries,
        series: seriesItems
      } = series;

      const {
        isBookFilesFetching,
        isBookFilesPopulated,
        bookFilesError,
        hasBookFiles
      } = bookFiles;

      const previousAuthor = sortedAuthor[authorIndex - 1] || _.last(sortedAuthor);
      const nextAuthor = sortedAuthor[authorIndex + 1] || _.first(sortedAuthor);
      const isAuthorRefreshing = isCommandExecuting(findCommand(commands, { name: commandNames.REFRESH_AUTHOR, authorId: author.id }));
      const authorRefreshingCommand = findCommand(commands, { name: commandNames.REFRESH_AUTHOR });
      const allAuthorRefreshing = (
        isCommandExecuting(authorRefreshingCommand) &&
        !authorRefreshingCommand.body.authorId
      );
      const isRefreshing = isAuthorRefreshing || allAuthorRefreshing;
      const isSearching = isCommandExecuting(findCommand(commands, { name: commandNames.AUTHOR_SEARCH, authorId: author.id }));
      const isRenamingFiles = isCommandExecuting(findCommand(commands, { name: commandNames.RENAME_FILES, authorId: author.id }));
      const isRenamingAuthorCommand = findCommand(commands, { name: commandNames.RENAME_AUTHOR });
      const isRenamingAuthor = (
        isCommandExecuting(isRenamingAuthorCommand) &&
        isRenamingAuthorCommand.body.authorIds.indexOf(author.id) > -1
      );

      const isFetching = isBooksFetching || isSeriesFetching || isBookFilesFetching;
      const isPopulated = isBooksPopulated && isSeriesPopulated && isBookFilesPopulated;

      const alternateTitles = _.reduce(author.alternateTitles, (acc, alternateTitle) => {
        if ((alternateTitle.seasonNumber === -1 || alternateTitle.seasonNumber === undefined) &&
            (alternateTitle.sceneSeasonNumber === -1 || alternateTitle.sceneSeasonNumber === undefined)) {
          acc.push(alternateTitle.title);
        }

        return acc;
      }, []);

      return {
        ...author,
        alternateTitles,
        isAuthorRefreshing,
        allAuthorRefreshing,
        isRefreshing,
        isSearching,
        isRenamingFiles,
        isRenamingAuthor,
        isFetching,
        isPopulated,
        booksError,
        isSaving,
        saveError,
        isDeleting,
        deleteError,
        seriesError,
        bookFilesError,
        hasBooks,
        hasMonitoredBooks,
        hasSeries,
        series: seriesItems,
        hasBookFiles,
        previousAuthor,
        nextAuthor,
        isSmallScreen: dimensions.isSmallScreen,
        pageSize,
        totalPages,
        totalRecords,
        page,
        books: rawBooks
      };
    }
  );
}

const mapDispatchToProps = {
  fetchSeries,
  clearSeries,
  saveBookEditor,
  fetchBookFiles,
  clearBookFiles,
  toggleAuthorMonitored,
  fetchQueueDetails,
  clearQueueDetails,
  clearReleases,
  cancelFetchReleases,
  executeCommand,
  fetchBooksByAuthor,
  clearBooks,
  gotoBooksFirstPage,
  gotoBooksPreviousPage,
  gotoBooksNextPage,
  gotoBooksLastPage,
  gotoBooksPage
};

class AuthorDetailsConnector extends Component {

  //
  // Lifecycle

  componentDidMount() {
    registerPagePopulator(this.populate);
    this.populate();
  }

  componentDidUpdate(prevProps) {
    const {
      id,
      isAuthorRefreshing,
      allAuthorRefreshing,
      isRenamingFiles,
      isRenamingAuthor
    } = this.props;

    if (
      (prevProps.isAuthorRefreshing && !isAuthorRefreshing) ||
      (prevProps.allAuthorRefreshing && !allAuthorRefreshing) ||
      (prevProps.isRenamingFiles && !isRenamingFiles) ||
      (prevProps.isRenamingAuthor && !isRenamingAuthor)
    ) {
      this.populate();
    }

    // If the id has changed we need to clear the books
    // files and fetch from the server.

    if (prevProps.id !== id) {
      this.unpopulate();
      this.populate();
    }
  }

  componentWillUnmount() {
    unregisterPagePopulator(this.populate);
    this.unpopulate();
  }

  //
  // Control

  populate = () => {
    const authorId = this.props.id;
    const { page = 1, pageSize = 50, books, statistics } = this.props;

    const authorBooks = books && books.items ? books.items.filter((book) => book.authorId === authorId) : [];
    const hasCompleteBookList = statistics && statistics.totalBookCount !== 0 && authorBooks.length >= statistics.totalBookCount;

    if (!hasCompleteBookList) {
      this.props.fetchBooksByAuthor({ authorId, page, pageSize });
    }

    this.props.fetchSeries({ authorId });
    this.props.fetchBookFiles({ authorId });
    this.props.fetchQueueDetails({ authorId });
  };

  unpopulate = () => {
    this.props.cancelFetchReleases();
    this.props.clearSeries();
    this.props.clearBookFiles();
    this.props.clearQueueDetails();
    this.props.clearReleases();
  };

  //
  // Listeners

  onMonitorTogglePress = (monitored) => {
    this.props.toggleAuthorMonitored({
      authorId: this.props.id,
      monitored
    });
  };

  onRefreshPress = () => {
    this.props.executeCommand({
      name: commandNames.REFRESH_AUTHOR,
      authorId: this.props.id
    });
  };

  onSearchPress = () => {
    this.props.executeCommand({
      name: commandNames.AUTHOR_SEARCH,
      authorId: this.props.id
    });
  };

  onSaveSelected = (payload) => {
    this.props.saveBookEditor(payload);
  };

  onFirstPagePress = () => {
    const { pageSize } = this.props;
    this.props.gotoBooksFirstPage({ authorId: this.props.id, pageSize });
  };

  onPreviousPagePress = () => {
    const { pageSize } = this.props;
    this.props.gotoBooksPreviousPage({ authorId: this.props.id, pageSize });
  };

  onNextPagePress = () => {
    const { pageSize } = this.props;
    this.props.gotoBooksNextPage({ authorId: this.props.id, pageSize });
  };

  onLastPagePress = () => {
    const { pageSize } = this.props;
    this.props.gotoBooksLastPage({ authorId: this.props.id, pageSize });
  };

  onPageSelect = (page) => {
    const { pageSize } = this.props;
    this.props.gotoBooksPage({ authorId: this.props.id, page, pageSize });
  };

  //
  // Render

  render() {
    return (
      <AuthorDetails
        {...this.props}
        onMonitorTogglePress={this.onMonitorTogglePress}
        onRefreshPress={this.onRefreshPress}
        onSearchPress={this.onSearchPress}
        onSaveSelected={this.onSaveSelected}
        onFirstPagePress={this.onFirstPagePress}
        onPreviousPagePress={this.onPreviousPagePress}
        onNextPagePress={this.onNextPagePress}
        onLastPagePress={this.onLastPagePress}
        onPageSelect={this.onPageSelect}
      />
    );
  }
}

AuthorDetailsConnector.propTypes = {
  id: PropTypes.number.isRequired,
  titleSlug: PropTypes.string.isRequired,
  isAuthorRefreshing: PropTypes.bool.isRequired,
  allAuthorRefreshing: PropTypes.bool.isRequired,
  isRefreshing: PropTypes.bool.isRequired,
  isRenamingFiles: PropTypes.bool.isRequired,
  isRenamingAuthor: PropTypes.bool.isRequired,
  fetchSeries: PropTypes.func.isRequired,
  clearSeries: PropTypes.func.isRequired,
  saveBookEditor: PropTypes.func.isRequired,
  fetchBookFiles: PropTypes.func.isRequired,
  clearBookFiles: PropTypes.func.isRequired,
  toggleAuthorMonitored: PropTypes.func.isRequired,
  fetchQueueDetails: PropTypes.func.isRequired,
  clearQueueDetails: PropTypes.func.isRequired,
  clearReleases: PropTypes.func.isRequired,
  cancelFetchReleases: PropTypes.func.isRequired,
  executeCommand: PropTypes.func.isRequired,
  fetchBooksByAuthor: PropTypes.func.isRequired,
  clearBooks: PropTypes.func.isRequired,
  gotoBooksFirstPage: PropTypes.func.isRequired,
  gotoBooksPreviousPage: PropTypes.func.isRequired,
  gotoBooksNextPage: PropTypes.func.isRequired,
  gotoBooksLastPage: PropTypes.func.isRequired,
  gotoBooksPage: PropTypes.func.isRequired,
  page: PropTypes.number,
  pageSize: PropTypes.number,
  books: PropTypes.object,
  statistics: PropTypes.object
};

export default connect(createMapStateToProps, mapDispatchToProps)(AuthorDetailsConnector);
